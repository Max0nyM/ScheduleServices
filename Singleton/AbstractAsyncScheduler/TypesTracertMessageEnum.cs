using System;
using System.Collections.Generic;
using System.Text;

namespace AbstractAsyncScheduler
{
    /// <summary>
    /// Типы статусов планировщика
    /// </summary>
    public enum StatusTypes
    {
        /// <summary>
        /// Стандартная установка статуса для информирования
        /// </summary>
        SetValueStatus,

        /// <summary>
        /// Статус об ошибке
        /// </summary>
        ErrorStatus
    }
}
