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
using ABCo.ABSave.Serialization.Core;
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

        public override void Serialize(in SerializeInfo info) =>
            Serialize(info.Instance, info.VersionInfo, info.Header);

        void Serialize(object instance, VersionInfo info, BitWriter header)
        {
            ObjectVersionInfo versionInfo = (ObjectVersionInfo)info;
            ObjectMemberSharedInfo[]? members = versionInfo.Members;
            ObjectConverter? baseType = versionInfo.BaseObject;

            if (baseType != null)
            {
                // TODO: Don't directly call this with map guides.
                ItemSerializer.HandleVersionNumber(baseType, out VersionInfo baseInfo, header);
                baseType.Serialize(instance, baseInfo, header);
            }

            if (members.Length == 0) return;

            // Serialize the rest.
            for (int i = 0; i < members.Length; i++)
                header.WriteItem(members[i].Accessor.Getter(instance), members[i].Map);
        }

        public override object Deserialize(in DeserializeInfo info)
        {
            object res = Activator.CreateInstance(info.ActualType)!;
            DeserializeInto(res, info.VersionInfo, info.Header);
            return res;
        }

        void DeserializeInto(object obj, VersionInfo info, BitReader header)
        {
            ObjectVersionInfo versionInfo = (ObjectVersionInfo)info;
            ObjectMemberSharedInfo[]? members = versionInfo.Members;
            ObjectConverter? baseType = versionInfo.BaseObject;

            if (baseType != null)
            {
                // TODO: Don't directly call this with map guides.
                VersionInfo baseInfo = header.ReadAndStoreVersionNumber(baseType);
                baseType.DeserializeInto(obj, baseInfo, header);
            }

            // Deserialize all the members that don't get the header.
            for (int i = 0; i < members.Length; i++)
                members[i].Accessor.Setter(obj, header.ReadItem(members[i].Map));
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
