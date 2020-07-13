namespace SmartProducts.Person.Domain.Enumerations
{
    public enum EventMessageType
    {
        /// <summary>
        /// 设备在线
        /// </summary>
        DeviceOnline = 100100305,
        /// <summary>
        /// 设备离线
        /// </summary>
        DeviceOutLine = 100100306,
        /// <summary>
        /// 周界入侵
        /// </summary>
        PerimeterInvasion = 100100105,
        /// <summary>
        /// 连接失败
        /// </summary>
        ConnectionFailed = 100100307,
        /// <summary>
        /// 布防失败
        /// </summary>
        ArmingFailed = 100100308,
        /// <summary>
        /// 验证通过
        /// </summary>
        Verified = 100100309,
        /// <summary>
        /// 未戴安全帽
        /// </summary>
        NotWearingHelmet = 100100310
    }
}
