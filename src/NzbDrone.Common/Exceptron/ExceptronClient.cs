﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using NzbDrone.Common.Exceptron.Configuration;
using NzbDrone.Common.Exceptron.Message;

namespace NzbDrone.Common.Exceptron
{
    public class ExceptronClient : IExceptronClient
    {
        internal IRestClient RestClient { private get; set; }

        /// <summary>
        /// Version of Client
        /// </summary>
        public string ClientVersion => Assembly.GetExecutingAssembly().GetName().Version.ToString();


        /// <summary>
        /// Name of Client
        /// </summary>
        public string ClientName => "Official .NET";

        /// <summary>
        /// Client Configuration
        /// </summary>
        public ExceptronConfiguration Configuration { get; private set; }


        /// <summary>
        /// Framework Type of the Host Application (.Net/mono)
        /// </summary>
        public string FrameworkType { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="ExceptronClient"/>
        /// Loads <see cref="ExceptronConfiguration"/> from application config file.
        /// </summary>
        /// <param name="applicationVersion">Version of the currently running application</param>
        public ExceptronClient(Version applicationVersion)
            : this(ExceptronConfiguration.ReadConfig(), applicationVersion)
        {
            FrameworkType = ".Net";
        }

        private readonly string _applicationVersion;
        private readonly string _maxFrameworkVersion;


        /// <param name="exceptronConfiguration">exceptron client configuration</param>
        /// <param name="applicationVersion"> </param>
        public ExceptronClient(ExceptronConfiguration exceptronConfiguration, Version applicationVersion)
        {
            if (exceptronConfiguration == null)
                throw new ArgumentNullException("exceptronConfiguration");

            if (applicationVersion == null)
                throw new ArgumentNullException("applicationVersion");

            if (string.IsNullOrEmpty(exceptronConfiguration.ApiKey))
                throw new ArgumentException("An API Key was not provided");

            Configuration = exceptronConfiguration;

            RestClient = new RestClient();

            _applicationVersion = applicationVersion.ToString();

            _maxFrameworkVersion = GetMaximumFrameworkVersion();

            FrameworkType = ".Net";
        }

        /// <summary>
        /// Submit an exception to exceptron Servers.
        /// </summary>
        /// <param name="exception">Exception that is being reported</param>
        /// <param name="component" 
        /// example="DataAccess, Configuration, Registration, etc." 
        /// remarks="It is common to use the logger name that was used to log the exception as the component.">Component that experienced this exception.</param>

        /// <param name="severity">Severity of the exception being reported</param>
        /// <param name="message" 
        /// example="Something went wrong while checking for application updates.">Any message that should be attached to this exceptions</param>
        /// <param name="userId"
        /// remarks="This Id does not have to be tied to the user's identity. 
        /// You can use a system generated unique ID such as GUID. 
        /// This field is used to report how many unique users are experiencing an error." 
        /// example="
        /// 62E5C8EF-0CA2-43AB-B278-FC6994F776ED
        /// Timmy@aol.com
        /// 26437
        /// ">ID that will uniquely identify the user</param>
        /// <param name="httpContext"><see cref="System.Web.HttpContext"/> in which the exception occurred. If no <see cref="System.Web.HttpContext"/> is provided
        /// <see cref="ExceptronClient"/> will try to get the current <see cref="System.Web.HttpContext"/> from <see cref="System.Web.HttpContext.Current"/></param>
        /// <returns></returns>
        public ExceptionResponse SubmitException(Exception exception, string component, ExceptionSeverity severity = ExceptionSeverity.None, string message = null, string userId = null)
        {
            var exceptionData = new ExceptionData
                                    {
                                        Exception = exception,
                                        Component = component,
                                        Severity = severity,
                                        Message = message,
                                        UserId = userId
                                    };

            return SubmitException(exceptionData);
        }

        /// <summary>
        /// Submit an exception to exceptron Servers.
        /// </summary>
        /// <param name="exceptionData">Exception data to be reported to the server</param>
        public ExceptionResponse SubmitException(ExceptionData exceptionData)
        {
            try
            {
                ValidateState(exceptionData);

                var report = new ExceptionReport();

                report.ap = Configuration.ApiKey;
                report.dn = ClientName;
                report.dv = ClientVersion;
                report.aver = _applicationVersion;

                report.ext = exceptionData.Exception.GetType().FullName;
                report.stk = ConvertToFrames(exceptionData.Exception);
                report.exm = exceptionData.Exception.Message;

                report.cmp = exceptionData.Component;
                report.uid = exceptionData.UserId;
                report.msg = exceptionData.Message;
                report.sv = (int)exceptionData.Severity;
                report.fv = _maxFrameworkVersion;
                report.ft = FrameworkType;

                SetEnviromentInfo(report);

                var exceptionResponse = RestClient.Put<ExceptionResponse>(Configuration.Host, report);

                exceptionData.Exception.Data["et"] = exceptionResponse.RefId;

                return exceptionResponse;
            }
            catch (Exception e)
            {
                Trace.WriteLine("Unable to submit exception to exceptron. ", e.ToString());

                if (Configuration.ThrowExceptions)
                {
                    //throw;
                }

                return new ExceptionResponse { Exception = e };
            }
        }

        private void ValidateState(ExceptionData exceptionData)
        {
            if (string.IsNullOrEmpty(Configuration.ApiKey))
                throw new InvalidOperationException("ApiKey has not been provided for this client.");

            if (exceptionData == null)
                throw new ArgumentNullException("exceptionData");

            if (exceptionData.Exception == null)
                throw new ArgumentException("ExceptionData.Exception Cannot be null.", "exceptionData");
        }

        private void SetEnviromentInfo(ExceptionReport report)
        {
            report.cul = Thread.CurrentThread.CurrentCulture.Name;

            if (string.IsNullOrEmpty(report.cul))
                report.cul = "en";

            try
            {
                report.os = Environment.OSVersion.VersionString;
            }
            catch (Exception)
            {
                if (Configuration.ThrowExceptions) throw;
            }

            if (Configuration.IncludeMachineName)
            {
                try
                {
                    report.hn = Environment.MachineName;
                }
                catch (Exception)
                {
                    if (Configuration.ThrowExceptions) throw;
                }
            }
        }



        internal static List<Frame> ConvertToFrames(Exception exception)
        {
            if (exception == null) return null;

            var stackTrace = new StackTrace(exception, true);

            var frames = stackTrace.GetFrames();

            if (frames == null) return null;

            var result = new List<Frame>();

            for (int index = 0; index < frames.Length; index++)
            {
                var frame = frames[index];
                var method = frame.GetMethod();
                var declaringType = method.DeclaringType;

                var fileName = frame.GetFileName();

                var currentFrame = new Frame
                {
                    i = index,
                    fn = fileName,
                    ln = frame.GetFileLineNumber(),
                    m = method.ToString(),
                };

                currentFrame.m = currentFrame.m.Substring(currentFrame.m.IndexOf(' ')).Trim();


                if (declaringType != null)
                {
                    currentFrame.c = declaringType.FullName;
                }

                result.Add(currentFrame);
            }


            return result;
        }

        private string GetMaximumFrameworkVersion()
        {
            var clrVersion = Environment.Version;

            if (clrVersion.Major == 2)
            {
                //Check if 2.0 or 3.5
                try
                {
                    Assembly.Load("System.Core, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
                    return "3.5";
                }
                catch (Exception)
                {
                }

                return "2.0";
            }

            if (clrVersion.Major == 4)
            {
                //Check if 4.0 or 4.5
                try
                {
                    Assembly.Load("System.Threading.Tasks.Parallel, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
                    return "4.5";
                }
                catch (Exception)
                {
                }

                return "4.0";
            }

            return "Unknown";
        }
    }
}
