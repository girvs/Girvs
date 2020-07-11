namespace Girvs.Domain.Configuration
{
    public class TaskConfig
    {
        /// <summary>
        /// 任务名称
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// 启用禁用
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// 程序集类型名称
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// 启用停止
        /// </summary>
        public bool EnableShutDown { get; set; }

        /// <summary>
        /// 任务出现异常后,重启间隔时间
        /// </summary>
        public int FailureInterval { get; set; }

        /// <summary>
        /// 任务出现异常后,重启次数
        /// </summary>
        public int NumberOfTries { get; set; }
        
        /// <summary>
        /// 是否为单线程任务
        /// </summary>
        public bool SingleThread { get; set; }

        /// <summary>
        /// Cron表达式
        /// </summary>
        public string CronExpression { get; set; }
    }
}





//<task name = "Publish" enabled = "true" type = "E3.Plugins.Articles.ArticleTask,E3.Plugins.Articles" enableShutDown = "false" failureInterval = "1" numberOfTries = "10" singleThread = "false" minutes = "1"  />