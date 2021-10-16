using System;
using System.Collections.Generic;
using ZhuoFan.Wb.BasicService.Domain.Enumerations;

namespace ZhuoFan.Wb.BasicService.Application.ViewModels.User
{
    public class BatchResetUserPasswordViewModel
    {
        public string NewPassword { get; set; }
        public IList<Guid> Ids { get; set; }
    }
}