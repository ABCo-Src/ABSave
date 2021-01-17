using ABSoftware.ABSave.Helpers;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace ABSoftware.ABSave.Mapping.Items
{
    internal struct ObjectFieldInfo
    {
        public Either<PropertyInfo, FieldInfo> Info;
        public MapItem Map;
    }

    internal class ObjectMapItem : MapItem
    {
        public ObjectFieldInfo[] Members;

        public ObjectMapItem(ObjectFieldInfo[] members, Type itemType, bool isValueType) => (Members, ItemType, IsValueType) = (members, itemType, isValueType);
    }
}
