using System;
using System.Collections.Generic;
using Girvs.AuthorizePermission.Enumerations;
using Girvs.Driven.Commands;

namespace ZhuoFan.Wb.BasicService.Domain.Commands.BasalPermission
{
    public class SaveBasalPermissionCommand : Command
    {
        public SaveBasalPermissionCommand(
            PermissionValidateObjectType validateObjectType,
            PermissionAppliedObjectType appliedObjectType, 
            Guid appliedId, 
            List<BasalPermissionDto> basalPermissionDtos)
        {
            BasalPermissionDtos = basalPermissionDtos;
            ValidateObjectType = validateObjectType;
            AppliedObjectType = appliedObjectType;
            AppliedID = appliedId;
        }

        public Guid AppliedID { get; private set; }
        public PermissionAppliedObjectType AppliedObjectType { get; private set; }
        public PermissionValidateObjectType ValidateObjectType { get; private set; }
        public List<BasalPermissionDto> BasalPermissionDtos { get; private set; }

        public override bool IsValid()
        {
            throw new System.NotImplementedException();
        }
        
        public override string CommandDesc { get; set; } = "保存权限";
    }

    public class BasalPermissionDto
    {
        public BasalPermissionDto()
        {
            Permissions = new List<Permission>();
        }
        public int ValidateObjectID { get; set; }
        public Guid AppliedObjectID { get; set; }
        
        public List<Permission> Permissions { get; set; }
    }
}