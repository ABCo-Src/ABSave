using ABCo.ABSave.Deserialization;
using ABCo.ABSave.Exceptions;
using ABCo.ABSave.Mapping;
using ABCo.ABSave.Mapping.Description;
using ABCo.ABSave.Mapping.Description.Attributes;
using ABCo.ABSave.Mapping.Description.Attributes.Converters;
using ABCo.ABSave.Mapping.Generation;
using ABCo.ABSave.Mapping.Generation.Converters;
using ABCo.ABSave.Mapping.Generation.IntermediateObject;
using ABCo.ABSave.Mapping.Generation.Object;
using ABCo.ABSave.Serialization;
using System;
using System.Reflection;

namespace ABCo.ABSave.Converters
{
    [SelectOtherWithCheckType]
    public class ObjectConverter : Converter
    {
        internal IntermediateObjectInfo _intermediateInfo;

        SaveMembersMode _saveMode;

        public override uint Initialize(InitializeInfo info)
        {
            return IntermediateMapper.CreateIntermediateObjectInfo(info.Type, _saveMode, out _intermediateInfo);
        }

        public override (VersionInfo?, bool) GetVersionInfo(InitializeInfo info, uint version)
        {
            ObjectMemberSharedInfo[]? members = _hasOneVersion ?
                ObjectVersionMapper.GenerateForOneVersion(this, info._gen) :
                ObjectVersionMapper.GenerateNewVersion(this, info._gen, version);

            SaveBaseMembersAttribute? attr = MappingHelpers.FindAttributeForVersion(_intermediateInfo.BaseMemberAttributes!, version);

            // If there is base for this version, get the converter for it.
            ObjectConverter? baseConv = null;
            if (attr != null)
                baseConv = GetBaseObjectConverter(info, attr);

            bool usesHeader = members.Length > 0 || baseConv != null;
            return (new ObjectVersionInfo(members, baseConv), usesHeader);
        }

        ObjectConverter GetBaseObjectConverter(InitializeInfo info, SaveBaseMembersAttribute attr)
        {
            if (!info.Type.IsSubclassOf(attr.BaseType))
                throw new InvalidSaveBaseMembersException($"The type {info.Type.Name} has an attribute on it saying that ABSave should serialize the base members {attr.BaseType.Name}, but the type doesn't inherit from this type anywhere in its inheritance chain! The attribute must describe a base type of the class.");

            // We'll try and get the map.
            // If what it gives back isn't an "ObjectConverter" or it threw "UnserializedTypeException",
            // then this object clearly ISN'T a "SaveMembers" type like it should be and as such we'll fail.
            try
            {
                var map = info.GetMap(attr.BaseType);
                if (map.Converter is ObjectConverter converter)
                    return converter;
            }
            catch (UnserializableTypeException) { }

            throw new InvalidSaveBaseMembersException($"The type {info.Type.Name} has an attribute on it saying that ABSave should serialize the base members {attr.BaseType.Name}, but this type doesn't have the 'SaveMembers' attribute on it, and it must for ABSave to serialize its members too.");
        }

        public override bool CheckType(CheckTypeInfo info) =>
            ObjectEligibilityChecker.CheckIfEligibleAndGetSaveMode(info.Type, out _saveMode);

        protected override void DoHandleAllVersionsGenerated() => _intermediateInfo.Release();

        // TODO: When map guides come along, instead of manually calling the temporarily internal
        // "ABSaveSerializer.HandleVersionNumber" ourselves, we should be able to communicate with the base
        // ObjectConverter that serialize into our instance as opposed to making a new one.
        public override void Serialize(in SerializeInfo info, ref BitTarget header) =>
            Serialize(info.Instance, info.VersionInfo, ref header);

        void Serialize(object instance, VersionInfo info, ref BitTarget header)
        {
            ObjectVersionInfo versionInfo = (ObjectVersionInfo)info;
            ObjectMemberSharedInfo[]? members = versionInfo.Members;
            ObjectConverter? baseType = versionInfo.BaseObject;

            if (baseType != null)
            {
                header.Serializer.HandleVersionNumber(baseType, out VersionInfo baseInfo, ref header);
                baseType.Serialize(instance, baseInfo, ref header);
            }

            if (members.Length == 0) return;

            // Serialize the first member.
            header.Serializer.SerializeItem(members[0].Accessor.Getter(instance), members[0].Map, ref header);

            // Serialize the rest.
            for (int i = 1; i < members.Length; i++)
                header.Serializer.SerializeItem(members[i].Accessor.Getter(instance), members[i].Map);
        }

        public override object Deserialize(in DeserializeInfo info, ref BitSource header)
        {
            object res = Activator.CreateInstance(info.ActualType)!;
            DeserializeInto(res, info.VersionInfo, ref header);
            return res;
        }

        void DeserializeInto(object obj, VersionInfo info, ref BitSource header)
        {
            ObjectVersionInfo versionInfo = (ObjectVersionInfo)info;
            ObjectMemberSharedInfo[]? members = versionInfo.Members;
            ObjectConverter? baseType = versionInfo.BaseObject;

            int deserializeWithoutHeaderStart;

            // If there's a base type to be serialized, the header goes to that,
            // and we'll deserialize the first member without the header.
            if (baseType == null)
            {
                deserializeWithoutHeaderStart = 1;

                if (members.Length == 0) return;

                // Deserialize the first member using the header
                members[0].Accessor.Setter(obj, header.Deserializer.DeserializeItem(members[0].Map, ref header));
            }
            else
            {

                header.Deserializer.HandleVersionNumber(baseType, out VersionInfo baseInfo, ref header);
                baseType.DeserializeInto(obj, baseInfo, ref header);

                // The header goes to the base type so we'll deserialize the first member in the loop of members that don't get the header.
                deserializeWithoutHeaderStart = 0;
            }

            // Deserialize all the members that don't get the header.
            for (int i = deserializeWithoutHeaderStart; i < members.Length; i++)
                members[i].Accessor.Setter(obj, header.Deserializer.DeserializeItem(members[i].Map));
        }

        internal class ObjectVersionInfo : VersionInfo
        {
            public ObjectMemberSharedInfo[] Members;
            public ObjectConverter? BaseObject;

            public ObjectVersionInfo(ObjectMemberSharedInfo[] members, ObjectConverter? baseObject) => 
                (Members, BaseObject) = (members, baseObject);
        }
    }
}
