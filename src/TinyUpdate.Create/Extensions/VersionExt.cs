﻿using SemVersion;

namespace TinyUpdate.Create.Extensions
{
    public static class VersionExt
    {
        /// <summary>
        /// Makes a <see cref="SemanticVersion"/> that is slightly lower then the current version
        /// </summary>
        /// <param name="version"></param>
        /// <returns></returns>
        public static SemanticVersion GetLowerVersion(this SemanticVersion version)
        {
            var maj = version.Major;
            var min = version.Minor;
            var pat = version.Patch;
            if (maj.GetValueOrDefault(0) > 0)
            {
                maj -= 1;
            }
            else if (min.GetValueOrDefault(0) > 0)
            {
                min -= 1;
            }
            else if (pat.GetValueOrDefault(0) > 0)
            {
                pat -= 1;
            }

            return new SemanticVersion(maj, min, pat, version.Prerelease, version.Build);
        }
    }
}