using System;
using System.Xml.Serialization;
using Girvs.AuthorizePermission.Enumerations;
using Girvs.BusinessBasis.Entities;

namespace BasicManagement.Domain.Models
{
    [Serializable]
    public abstract class PermissionBase : AggregateRoot<Guid>
    {
        #region Private Members

        //Permission
        Permission allowMask;
        Permission denyMask;

        #endregion

        public virtual Role Role => new Role();

        #region Cnstr

        public PermissionBase()
        {
            allowMask = 0;
            denyMask = (Permission)(long)-1;
        }

        #endregion

        #region Core Public Properties

        [XmlAttribute(AttributeName = "allowMask", DataType = "long")]
        public virtual Permission AllowMask
        {
            get { return allowMask; }
            set { allowMask = value; }
        }

        [XmlAttribute(AttributeName = "denyMask", DataType = "long")]
        public virtual Permission DenyMask
        {
            get { return denyMask; }
            set { denyMask = value; }
        }

        #endregion

        #region GetBit
        public virtual bool GetBit(Permission mask)
        {
            bool bReturn = false;

            if ((denyMask & mask) == mask)
                bReturn = false;

            if ((allowMask & mask) == mask)
                bReturn = true;

            return bReturn;
        }
        #endregion

        #region Public Methods

        public virtual void SetBit(Permission mask, AccessControlEntry accessControl)
        {

            switch (accessControl)
            {
                case AccessControlEntry.Allow:
                    allowMask |= (Permission)((long)mask & (long)-1);
                    denyMask &= ~(Permission)((long)mask & (long)-1);
                    break;
                case AccessControlEntry.NotSet:
                    allowMask &= ~(Permission)((long)mask & (long)-1);
                    denyMask &= ~(Permission)((long)mask & (long)-1);
                    break;
                default:
                    allowMask &= ~(Permission)((long)mask & (long)-1);
                    denyMask |= (Permission)((long)mask & (long)-1);
                    break;
            }
        }

        public virtual void Merge(Permission p)
        {
            this.allowMask |= p;
#if DEBUG
            //��������Բ��С�
#endif
            this.denyMask &= p;
        }

        public virtual void Merge(PermissionBase permissionBase)
        {
            this.allowMask |= permissionBase.AllowMask;
            this.denyMask |= permissionBase.DenyMask;
        }

        public override bool Equals(object obj)
        {
            bool isEqual = true;
            PermissionBase permissionBase = obj as PermissionBase;
            if (permissionBase == null && this != null)
                return isEqual;

            foreach (Permission permission in Enum.GetValues(typeof(Permission)))
            {
                if (permissionBase.GetBit(permission) != this.GetBit(permission))
                {
                    isEqual = false;
                    break;
                }
            }

            return isEqual;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion

    }

    //public delegate void AccessCheckDelegate(IPlugin app, Permission permission, User user);

    //public delegate bool ValidatePermissionsDelegate(IPlugin app, Permission permission, User user);

}
