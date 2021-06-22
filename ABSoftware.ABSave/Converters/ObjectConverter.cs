using ABCo.ABSave.Deserialization;
using ABCo.ABSave.Mapping.Generation;
using ABCo.ABSave.Serialization;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using ABCo.ABSave.Mapping.Description;
using ABCo.ABSave.Mapping.Description.Attributes;
using ABCo.ABSave.Mapping.Generation.IntermediateObject;
using ABCo.ABSave.Mapping;
using ABCo.ABSave.Mapping.Description.Attributes.Converters;
using ABCo.ABSave.Mapping.Generation.Object;

namespace ABCo.ABSave.Converters
{
    [SelectOtherWithCheckType]
    public class ObjectConverter : Converter
    {
        internal ObjectIntermediateItem[]? _rawMembers;
        SaveMembersMode _saveMode;

        public override void Initialize(InitializeInfo info)
        {
            HighestVersion = IntermediateMapper.CreateIntermediateObjectInfo(info.Type, _saveMode, out _rawMembers);
        }

        public override (VersionInfo?, bool) GetVersionInfo(InitializeInfo info, uint version)
        {
            var members = HasOneVersion ?
                ObjectVersionMapper.GenerateForOneVersion(this, info._gen) :
                ObjectVersionMapper.GenerateNewVersion(this, info._gen, version);

            return (new ObjectVersionInfo(members), true);
        }

        public override bool CheckType(CheckTypeInfo info)
        {
            var attribute = info.Type.GetCustomAttribute<SaveMembersAttribute>(false);
            if (attribute == null) return false;

            _saveMode = attribute.Mode;
            return true;
        }

        protected override void DoHandleAllVersionsGenerated() => _rawMembers = null;

        public override void Serialize(in SerializeInfo info, ref BitTarget header)
        {
            var instance = info.Instance;
            var members = ((ObjectVersionInfo)info.VersionInfo).Members;

            for (int i = 0; i < members.Length; i++)
                header.Serializer.SerializeItem(members[i].Accessor.Getter(instance), members[i].Map);
        }

        public override object Deserialize(in DeserializeInfo info, ref BitSource header)
        {
            var res = Activator.CreateInstance(info.ActualType);
            var members = ((ObjectVersionInfo)info.VersionInfo).Members;

            for (int i = 0; i < members.Length; i++)
                members[i].Accessor.Setter(res!, header.Deserializer.DeserializeItem(members[i].Map));

            return res!;
        }

        class ObjectVersionInfo : VersionInfo
        {
            public ObjectMemberSharedInfo[] Members;

            public ObjectVersionInfo(ObjectMemberSharedInfo[] members) => Members = members;
        }
    }
}
