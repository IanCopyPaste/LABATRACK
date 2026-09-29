using System;

namespace LABATRACK.Services
{
    /// <summary>
    /// Thrown by a service when a shop rule says no (cash below the total, a weight above the
    /// maximum, a job someone else already moved). The message is written for the person at the
    /// counter, so a page can catch it and show it as it is. Nothing is saved when it is thrown.
    /// </summary>
    public class BusinessRuleException : Exception
    {
        public BusinessRuleException(string message) : base(message)
        {
        }
    }
}
