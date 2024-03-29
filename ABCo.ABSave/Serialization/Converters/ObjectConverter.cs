﻿using ABCo.ABSave.Exceptions;
using ABCo.ABSave.Helpers;
using ABCo.ABSave.Mapping;
using ABCo.ABSave.Mapping.Description;
using ABCo.ABSave.Mapping.Description.Attributes;
using ABCo.ABSave.Mapping.Description.Attributes.Converters;
using ABCo.ABSave.Mapping.Generation;
using ABCo.ABSave.Mapping.Generation.Converters;
using ABCo.ABSave.Mapping.Generation.IntermediateObject;
using ABCo.ABSave.Mapping.Generation.Object;
using ABCo.ABSave.Serialization.Reading;
using ABCo.ABSave.Serialization.Writing;
using System;

namespace ABCo.ABSave.Serialization.Converters
{
	[SelectOtherWithCheckType]
    public class ObjectConverter : Converter
    {
        internal IntermediateObjectInfo _intermediateInfo;

        SaveMembersMode _saveMode;
        bool _isAbstract;

        public override uint Initialize(InitializeInfo info)
        {
	        _isAbstract = info.Type.IsAbstract;

	        if (!info.Type.HasEmptyOrDefaultConstructor())
		        throw new UnsupportedTypeException(info.Type, "Type does not have a parameterless constructor");
                
	        return IntermediateMapper.CreateIntermediateObjectInfo(info.Type, _saveMode, out _intermediateInfo);
        }

        public override VersionInfo? GetVersionInfo(InitializeInfo info, uint version)
        {
            ObjectMemberSharedInfo[]? members = _hasOneVersion ?
                ObjectVersionMapper.GenerateForOneVersion(this, info._gen) :
                ObjectVersionMapper.GenerateNewVersion(this, info._gen, version);

            SaveBaseMembersAttribute? attr = MappingHelpers.FindAttributeForVersion(_intermediateInfo.BaseMemberAttributes!, version);

            // If there is base for this version, get the converter for it.
            ObjectConverter? baseConv = null;
            if (attr != null)
                baseConv = GetBaseObjectConverter(info, attr);

            return new ObjectVersionInfo(members, baseConv);
        }

        static ObjectConverter GetBaseObjectConverter(InitializeInfo info, SaveBaseMembersAttribute attr)
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
            Serialize(info.Instance, info.ActualType, info.VersionInfo, info.Serializer);

        void Serialize(object instance, Type actualType, VersionInfo info, ABSaveSerializer serializer)
        {
	        if (_isAbstract) throw new UnsupportedTypeException(actualType, "Type is abstract");

            ObjectVersionInfo versionInfo = (ObjectVersionInfo)info;
            ObjectMemberSharedInfo[]? members = versionInfo.Members;
            ObjectConverter? baseType = versionInfo.BaseObject;

            if (baseType != null)
            {
                var baseInfo = serializer.WriteVersionInfo(baseType);
                Serialize(instance, actualType, baseInfo!, serializer);
            }

            if (members.Length == 0) return;

            // Serialize the rest.
            for (int i = 0; i < members.Length; i++)
                serializer.WriteItem(members[i].Accessor.Getter(instance), members[i].Map);
        }

        public override object Deserialize(in DeserializeInfo info)
        {
	        if (_isAbstract) throw new UnsupportedTypeException(info.ActualType, "Type is abstract");

            object res = Activator.CreateInstance(info.ActualType)!;
            DeserializeInto(res, info.VersionInfo, info.Deserializer);
            return res;
        }

        static void DeserializeInto(object obj, VersionInfo info, ABSaveDeserializer deserializer)
        {
            ObjectVersionInfo versionInfo = (ObjectVersionInfo)info;
            ObjectMemberSharedInfo[]? members = versionInfo.Members;
            ObjectConverter? baseType = versionInfo.BaseObject;

            if (baseType != null)
            {
                VersionInfo baseInfo = deserializer.ReadVersionInfo(baseType);
                DeserializeInto(obj, baseInfo, deserializer);
            }

            // Deserialize all the members that don't get the header.
            for (int i = 0; i < members.Length; i++)
                members[i].Accessor.Setter(obj, deserializer.ReadItem(members[i].Map));
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
