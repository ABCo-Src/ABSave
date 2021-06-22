using ABCo.ABSave.Mapping.Generation.IntermediateObject;
using System;
using System.Collections.Generic;
using System.Text;

namespace ABCo.ABSave.Mapping.Generation.Object
{
    /// <summary>
    /// In charge of initially creating an object's map
    /// </summary>
    internal static class ObjectInitialMapper
    {
        public static MapItem GenerateNewObject(MapGenerator gen, Type type)
        {
            var res = new ObjectMapItem();
            gen.ApplyItem(res, type);

            res.HighestVersion = IntermediateMapper.CreateIntermediateObjectInfo(type, ref res.Intermediate);

            if (res.Intermediate.RawMembers!.Length == 0)
                FillDestWithNoMembers(res);
            else if (res.HighestVersion == 0)
                FillDestWithOneVersion(res);
            else
                FillDestWithMultipleVersions(res);

            return res;
        }

        static void FillDestWithNoMembers(ObjectMapItem dest)
        {
            dest.Intermediate.Release();

            dest.HasOneVersion = true;
            dest.HighestVersion = 0;
            dest.Members.OneVersion = 
                new ObjectVersionInfo(Array.Empty<ObjectMemberSharedInfo>(), null);
        }

        static void FillDestWithMultipleVersions(MapGenerator gen, ObjectMapItem dest)
        {
            dest.HasOneVersion = false;
            dest.Members.MultipleVersions = new Dictionary<uint, ObjectVersionInfo>();

            // Generate the highest version.
            ObjectVersionHandler.AddNewVersion(gen, dest.HighestVersion, dest);
        }

        static void FillDestWithOneVersion(MapGenerator gen, ObjectMapItem dest)
        {
            dest.HasOneVersion = true;
            dest.Members.OneVersion = ObjectVersionMapper.GenerateForOneVersion(gen, dest.HighestVersion, dest);

            // There are no more versions here, drop the raw members.
            dest.Intermediate.Release();
        }
    }
}
