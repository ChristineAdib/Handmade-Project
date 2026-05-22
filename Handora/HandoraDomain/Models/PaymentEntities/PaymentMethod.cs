using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraDomain.Models.PaymentEntities
{
    public enum PaymentMethod
    {
        CreditCard = 1,
        DebitCard = 2,
        Cash = 3,
        BankTransfer = 4
    }
}
