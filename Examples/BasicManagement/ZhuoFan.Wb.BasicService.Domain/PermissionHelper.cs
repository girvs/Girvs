using System;
using System.Collections.Generic;
using System.Linq;
using Girvs.AuthorizePermission;
using Girvs.AuthorizePermission.Enumerations;
using ZhuoFan.Wb.BasicService.Domain.Models;

namespace ZhuoFan.Wb.BasicService.Domain
{
    public static class PermissionHelper
    {
        public static int ConvertStringToPermissionValue(List<string> permissionStrs)
        {
            var validateObjectID = Permission.Undefined;
            foreach (var permissionStr in permissionStrs)
            {
                Permission c = (Permission)Enum.Parse(typeof(Permission), permissionStr, true);
                validateObjectID = validateObjectID | c;
            }

            return (int)validateObjectID;
        }

        public static List<Permission> ConvertStringToPermission(List<string> permissionStrs)
        {
            List<Permission> permissions = new List<Permission>();
            foreach (var permissionStr in permissionStrs)
            {
                var permission = (Permission)Enum.Parse(typeof(Permission), permissionStr, true);
                permissions.Add(permission);
            }

            return permissions;
        }

        public static List<string> ConvertPermissionToString(BasalPermission basalPermission)
        {
            var list = new List<string>();
            foreach (Permission value in typeof(Permission).GetEnumValues())
            {
                if (basalPermission.GetBit(value))
                {
                    list.Add(value.ToString());
                }
            }

            return list;
        }

        public static List<BasalPermission> MergeValidateObjectTypePermission(List<BasalPermission> ps)
        {
            var psGroup = ps.GroupBy(p => p.AppliedObjectID).ToList();
            var newPs = new List<BasalPermission>();
            foreach (var item in psGroup)
            {
                if (!item.Any()) continue;
                var allowMask = Permission.Undefined;
                var denyMask = Permission.Undefined;
                foreach (var p in item)
                {
                    allowMask |= p.AllowMask;
                    denyMask |= p.DenyMask;
                }

                var m = item.FirstOrDefault();
                m.AllowMask = allowMask;
                m.DenyMask = denyMask;
                newPs.Add(m);
            }

            return newPs;
        }


        public static List<AuthorizeDataRuleModel> ConvertAuthorizeDataRuleModels(List<UserRule> rulesList)
        {
            var authorizeDataRuleModels = new List<AuthorizeDataRuleModel>();

            if (rulesList == null || !rulesList.Any()) return authorizeDataRuleModels;

            foreach (var authorizeDataRule in rulesList)
            {
                var existReturnReuslt =
                    authorizeDataRuleModels.FirstOrDefault(
                        x => x.EntityTypeName == authorizeDataRule.EntityTypeName) ??
                    new AuthorizeDataRuleModel()
                    {
                        EntityTypeName = authorizeDataRule.EntityTypeName,
                        EntityDesc = authorizeDataRule.EntityDesc
                    };

                existReturnReuslt.AuthorizeDataRuleFieldModels.Add(new AuthorizeDataRuleFieldModel()
                {
                    ExpressionType = authorizeDataRule.ExpressionType,
                    FieldType = authorizeDataRule.FieldType,
                    FieldName = authorizeDataRule.FieldName,
                    FieldValue = authorizeDataRule.FieldValue,
                    FieldDesc = authorizeDataRule.FieldDesc
                });

                authorizeDataRuleModels.Add(existReturnReuslt);
            }

            return authorizeDataRuleModels;
        }
    }
}