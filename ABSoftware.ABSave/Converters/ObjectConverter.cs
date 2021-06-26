using ABCo.ABSave.Deserialization;
using ABCo.ABSave.Exceptions;
using ABCo.ABSave.Mapping;
using ABCo.ABSave.Mapping.Description;
using ABCo.ABSave.Mapping.Description.Attributes;
using ABCo.ABSave.Mapping.Description.Attributes.Converters;
using ABCo.ABSave.Mapping.Generation;
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

        // If the attribute "SaveBaseMembersAttribute" is present, we need to get the object converter
        // for that base so we can serialize it.
        public ObjectConverter? ObjectBaseType;

        SaveMembersMode _saveMode;

        public override void Initialize(InitializeInfo info)
        {
            HighestVersion = IntermediateMapper.CreateIntermediateObjectInfo(info.Type, _saveMode, out _intermediateInfo);

            // Set the "ObjectBaseType" by going through all the base types and finding the first one
            // that's object converted and using that.
            Type? currentType = info.Type.BaseType;
            while (currentType != null)
            {
                if (ObjectEligibilityChecker.IsEligible(info.Type))
                {
                    ObjectBaseType = (ObjectConverter)info.GetMap(info.Type).InnerItem;
                    break;
                }

                currentType = currentType.BaseType;
            }
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

            return (new ObjectVersionInfo(members, baseConv), true);
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
                var map = info.GetMap(info.Type);
                if (map.InnerItem is ObjectConverter converter)
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
        public override void Serialize(in SerializeInfo info, ref BitTarget header)
        {
            object? instance = info.Instance;
            ObjectMemberSharedInfo[]? members = ((ObjectVersionInfo)info.VersionInfo).Members;

            for (int i = 0; i < members.Length; i++)
                header.Serializer.SerializeItem(members[i].Accessor.Getter(instance), members[i].Map, ref header);
        }

        public override object Deserialize(in DeserializeInfo info, ref BitSource header)
        {
            object? res = Activator.CreateInstance(info.ActualType);
            ObjectMemberSharedInfo[]? members = ((ObjectVersionInfo)info.VersionInfo).Members;

            for (int i = 0; i < members.Length; i++)
                members[i].Accessor.Setter(res!, header.Deserializer.DeserializeItem(members[i].Map));

            return res!;
        }

        internal class ObjectVersionInfo : VersionInfo
        {
            public ObjectMemberSharedInfo[] Members;
            public ObjectConverter? BaseObject;

            public ObjectVersionInfo(ObjectMemberSharedInfo[] members, ObjectConverter? baseObject) => 
                Members = members;
        }
    }
}
