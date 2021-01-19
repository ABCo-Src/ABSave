using ABSoftware.ABSave.Helpers;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace ABSoftware.ABSave.Mapping.Items
{
    internal struct ObjectMemberInfo
    {
        public Either<FieldInfo, PropertyMapInfo> Info;
        public MapItem Map;
    }

    internal struct PropertyMapInfo
    {
        public Func<object, object> Getter;
        public Action<object, object> Setter;
    }

    internal class ObjectMapItem : MapItem
    {
        public ObjectMemberInfo[] Members;

        public ObjectMapItem(ObjectMemberInfo[] members, MapItemType itemType) => (Members, ItemType) = (members, itemType);
    }
}
