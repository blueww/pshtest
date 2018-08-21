using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if !DOTNET5_4
using Microsoft.Wtt.Log;
#endif

namespace MS.Test.Common.MsTestLib
{
    public class WTTLogger : ILogger
    {
        private const string DEFAULT_INIT_STRING = "$console";
        private WttLog m_Logger = null;

        /// 
        /// <summary>
        /// Creates a Wtt Logger (2.1) object with default settings.
        /// </summary>
        /// 
        public WTTLogger()
        {
            m_Logger = new WttLog(DEFAULT_INIT_STRING);
        }

        /// 
        /// <summary>
        /// Creates a Wtt Logger (2.0) object with the supplied settings. If initialization
        /// string is null, then default settings are applied.
        /// </summary>
        /// <param name="initString">initialization string. For more info please refer to 
        /// WTT Logger 2.0 documentation</param>
        /// 
        public WTTLogger(string initString)
        {
            if (initString == null)
            {
                m_Logger = new WttLog(DEFAULT_INIT_STRING);
            }
            else
            {
                Console.WriteLine("WTTLogger.WTTLogger {0}", initString);
                m_Logger = new WttLog(initString);
            }
        }

        
        ///
        /// <summary>
        /// Implementation of ILoggerAdapter interface
        /// </summary>
        /// <see cref="MS.Test.Common.LoggerFramework.ILoggerAdapter"/>
        /// <param name="msg">Format message string</param>
        /// <param name="exp">exception object</param>
        /// <param name="objToLog">Objects that need to be serialized in the message</param>
        /// 
        public void WriteError(
            string msg,
            params object[] objToLog)
        {
            //wtt log cannot exceed the length of 2048 per item
            StringBuilder sBuilder = new StringBuilder(MessageBuilder.FormatString(msg, objToLog));
            for (int start = 0; start < sBuilder.Length - 1; start += 2408)
            {
                int remainLength = sBuilder.Length - start;
                if (remainLength > 2408)
                {
                    m_Logger.Write(Level.Error, sBuilder.ToString(start, 2408));

                }
                else
                {
                    m_Logger.Write(Level.Error, sBuilder.ToString(start, remainLength));
                    break;
                }

            }

        }


        /// 
        /// <summary>
        /// Implementation of ILoggerAdapter interface
        /// </summary>
        /// <see cref="MS.Test.Common.LoggerFramework.ILoggerAdapter"/>
        /// <param name="msg">Format message string</param>
        /// <param name="objToLog">Objects that need to be serialized in the message</param>
        /// 
        public void WriteWarning(
            string msg,
            params object[] objToLog)
        {
            //wtt log cannot exceed the length of 2048 per item
            StringBuilder sBuilder = new StringBuilder(MessageBuilder.FormatString(msg, objToLog));
            for (int start = 0; start < sBuilder.Length - 1; start += 2408)
            {
                int remainLength = sBuilder.Length - start;
                if (remainLength > 2408)
                {
                    m_Logger.Write(Level.Warn, sBuilder.ToString(start, 2408));

                }
                else
                {
                    m_Logger.Write(Level.Warn, sBuilder.ToString(start, remainLength));
                    break;
                }

            }
        }

        /// 
        /// <summary>
        /// Implementation of ILoggerAdapter interface
        /// </summary>
        /// <see cref="MS.Test.Common.LoggerFramework.ILoggerAdapter"/>
        /// <param name="msg">Format message string</param>
        /// <param name="objToLog">Objects that need to be serialized in the message</param>
        /// 
        public void WriteInfo(
            string msg,
            params object[] objToLog)
        {
            //wtt log cannot exceed the length of 2048 per item
            StringBuilder sBuilder = new StringBuilder(MessageBuilder.FormatString(msg, objToLog));
            for (int start = 0; start < sBuilder.Length - 1; start += 2408)
            {
                int remainLength = sBuilder.Length - start;
                if (remainLength > 2408)
                {
                    m_Logger.Write(Level.Info, sBuilder.ToString(start, 2408));
                }
                else
                {
                    m_Logger.Write(Level.Info, sBuilder.ToString(start, remainLength));
                    break;
                }
                
            }

        }


        /// 
        /// <summary>
        /// Implementation of ILoggerAdapter interface
        /// </summary>
        /// <see cref="MS.Test.Common.LoggerFramework.ILoggerAdapter"/>
        /// <param name="msg">Format message string</param>
        /// <param name="objToLog">Objects that need to be serialized in the message</param>
        /// 
        public void WriteVerbose(
            string msg,
            params object[] objToLog)
        {
            //wtt log cannot exceed the length of 2048 per item
            StringBuilder sBuilder = new StringBuilder(MessageBuilder.FormatString(msg, objToLog));
            for (int start = 0; start < sBuilder.Length - 1; start += 2408)
            {
                int remainLength = sBuilder.Length - start;
                if (remainLength > 2408)
                {
                    m_Logger.Write(Level.Info, sBuilder.ToString(start, 2408));

                }
                else
                {
                    m_Logger.Write(Level.Info, sBuilder.ToString(start, remainLength));
                    break;
                }

            }

        }


        ///
        /// <summary>
        /// Implementation of ILoggerAdapter interface
        /// </summary>
        /// <see cref="MS.Test.Common.LoggerFramework.ILoggerAdapter"/>
        /// <param name="testId">Test id</param>
        /// 
        public void StartTest(
            string testId)
        {
            m_Logger.StartTest(testId);
        }

        /// 
        /// <summary>
        /// Implementation of ILoggerAdapter interface
        /// </summary>
        /// <see cref="MS.Test.Common.LoggerFramework.ILoggerAdapter"/>
        /// <param name="testId">Test id</param>
        /// <param name="executionTime">Execution time of the Test</param>
        /// <param name="result">Result of the Test</param>        
        /// 
        public void EndTest(
            string testId, 
            TimeSpan executionTime,
            TestResult result)
        {
            m_Logger.EndTest(testId);
        }

        /// 
        /// <summary>
        /// Implementation of ILoggerAdapter interface
        /// </summary>
        /// <see cref="MS.Test.Common.LoggerFramework.ILoggerAdapter"/>
        /// <returns>WTT logger object</returns>
        /// 
        public object GetLogger()
        {
            return m_Logger;
        }

        /// 
        /// <summary>
        /// Implementation of ILoggerAdapter interface
        /// </summary>
        /// <see cref="MS.Test.Common.LoggerFramework.ILoggerAdapter"/>
        /// 
        public void Close()
        {
            m_Logger.Dispose();
        }

        /// 
        /// <summary>
        /// Frees all resources that are held.
        /// </summary>
        /// 
        public void Dispose()
        {
            Close();
        }

    }
}
