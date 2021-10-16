using Girvs.EventBus;

namespace ZhuoFan.Wb.Common.Events.Workflows
{
    public class UpdatePaymentStatusEvent : IntegrationEvent
    {
        public UpdatePaymentStatusEvent()
        {

        }
        public UpdatePaymentStatusEvent(int? paymentResult, string paymentNo)
        {
            PaymentResult = paymentResult;
            PaymentNo = paymentNo;
        }
        /// <summary>
        /// 支付结果
        /// 0.待支付 1.待确认 2.支付成功 3.支付失败4.支付取消 5.已退款
        /// </summary>
        public int? PaymentResult { get; set; }

        /// <summary>
        /// 支付订单号
        /// </summary>
        public string PaymentNo { get; set; }
    }
}
