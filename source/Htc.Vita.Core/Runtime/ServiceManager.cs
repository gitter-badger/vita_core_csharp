﻿using System;
using System.Runtime.InteropServices;
using Htc.Vita.Core.Interop;
using Htc.Vita.Core.Log;

namespace Htc.Vita.Core.Runtime
{
    public class ServiceManager
    {
        private static readonly Logger Log = Logger.GetInstance(typeof(ServiceManager));

        public static ServiceInfo ChangeStartType(string serviceName, StartType startType)
        {
            return ChangeStartTypeInWindows(serviceName, startType);
        }

        public static bool CheckIfExists(string serviceName)
        {
            return CheckIfExistsInWindows(serviceName);
        }

        public static ServiceInfo QueryStartType(string serviceName)
        {
            return QueryStartTypeInWindows(serviceName);
        }

        public static ServiceInfo Start(string serviceName)
        {
            return StartInWindows(serviceName);
        }

        private static ServiceInfo ChangeStartTypeInWindows(
                string serviceName,
                StartType startType)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
            {
                return new ServiceInfo
                {
                        ServiceName = serviceName,
                        ErrorCode = Windows.ERROR_INVALID_NAME,
                        ErrorMessage = "Service name \"" + serviceName + "\" is invalid"
                };
            }

            var managerHandle = Windows.Advapi32.OpenSCManagerW(
                    null,
                    null,
                    Windows.Advapi32.ServiceControlManagerAccessRight.Connect
            );
            if (managerHandle == IntPtr.Zero)
            {
                var errorCode = Marshal.GetLastWin32Error();
                return new ServiceInfo
                {
                        ServiceName = serviceName,
                        ErrorCode = errorCode,
                        ErrorMessage = "Can not open Windows service controller manager, error code: " + errorCode
                };
            }

            var serviceInfo = new ServiceInfo
            {
                    ServiceName = serviceName,
                    StartType = startType
            };
            var serviceHandle = Windows.Advapi32.OpenServiceW(
                    managerHandle,
                    serviceName,
                    Windows.Advapi32.ServiceAccessRight.ChangeConfig |
                            Windows.Advapi32.ServiceAccessRight.QueryConfig |
                            Windows.Advapi32.ServiceAccessRight.QueryStatus
            );
            if (serviceHandle == IntPtr.Zero)
            {
                var errorCode = Marshal.GetLastWin32Error();
                serviceInfo.ErrorCode = errorCode;
                serviceInfo.ErrorMessage = "Can not open Windows service \"" + serviceName + "\", error code: " + errorCode;
            }
            else
            {
                var success = Windows.Advapi32.ChangeServiceConfigW(
                        serviceHandle,
                        Windows.Advapi32.ServiceType.NoChange,
                        ConvertToWindows(startType),
                        Windows.Advapi32.ErrorControlType.NoChange,
                        null,
                        null,
                        IntPtr.Zero,
                        null,
                        null,
                        null,
                        null
                );
                if (!success)
                {
                    var errorCode = Marshal.GetLastWin32Error();
                    serviceInfo.ErrorCode = errorCode;
                    serviceInfo.ErrorMessage = "Can not change Windows service \"" + serviceName + "\" config, error code: " + errorCode;
                }

                serviceInfo = UpdateCurrentStateInWindows(serviceHandle, serviceInfo);

                Windows.Advapi32.CloseServiceHandle(serviceHandle);
            }

            Windows.Advapi32.CloseServiceHandle(managerHandle);
            return serviceInfo;
        }

        private static bool CheckIfExistsInWindows(string serviceName)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
            {
                return false;
            }

            var managerHandle = Windows.Advapi32.OpenSCManagerW(
                    null,
                    null,
                    Windows.Advapi32.ServiceControlManagerAccessRight.Connect
            );
            if (managerHandle == IntPtr.Zero)
            {
                var errorCode = Marshal.GetLastWin32Error();
                Log.Error("Can not open Windows service controller manager, error code: " + errorCode);
                return false;
            }

            var serviceHandle = Windows.Advapi32.OpenServiceW(
                    managerHandle,
                    serviceName,
                    Windows.Advapi32.ServiceAccessRight.QueryConfig
            );
            if (serviceHandle == IntPtr.Zero)
            {
                var errorCode = Marshal.GetLastWin32Error();
                if (errorCode != Windows.ERROR_SERVICE_DOES_NOT_EXIST)
                {
                    Log.Error("Can not open Windows service \"" + serviceName + "\", error code: " + errorCode);
                }
                return false;
            }

            Windows.Advapi32.CloseServiceHandle(serviceHandle);
            return true;
        }

        private static ServiceInfo QueryStartTypeInWindows(string serviceName)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
            {
                return new ServiceInfo
                {
                        ServiceName = serviceName,
                        ErrorCode = Windows.ERROR_INVALID_NAME,
                        ErrorMessage = "Service name \"" + serviceName + "\" is invalid"
                };
            }

            var managerHandle = Windows.Advapi32.OpenSCManagerW(
                    null,
                    null,
                    Windows.Advapi32.ServiceControlManagerAccessRight.Connect
            );
            if (managerHandle == IntPtr.Zero)
            {
                var errorCode = Marshal.GetLastWin32Error();
                return new ServiceInfo
                {
                        ServiceName = serviceName,
                        ErrorCode = errorCode,
                        ErrorMessage = "Can not open Windows service controller manager, error code: " + errorCode
                };
            }

            var serviceInfo = new ServiceInfo
            {
                    ServiceName = serviceName
            };
            var serviceHandle = Windows.Advapi32.OpenServiceW(
                    managerHandle,
                    serviceName,
                    Windows.Advapi32.ServiceAccessRight.QueryConfig | Windows.Advapi32.ServiceAccessRight.QueryStatus
            );
            if (serviceHandle == IntPtr.Zero)
            {
                var errorCode = Marshal.GetLastWin32Error();
                serviceInfo.ErrorCode = errorCode;
                serviceInfo.ErrorMessage = "Can not open Windows service \"" + serviceName + "\", error code: " + errorCode;
            }
            else
            {
                const uint bytesAllocated = 8192;
                var serviceConfigPtr = Marshal.AllocHGlobal((int)bytesAllocated);
                try
                {
                    uint bytes;
                    var success = Windows.Advapi32.QueryServiceConfigW(
                            serviceHandle,
                            serviceConfigPtr,
                            bytesAllocated,
                            out bytes
                    );
                    if (success)
                    {
                        var serviceConfig = (Windows.Advapi32.QueryServiceConfig) Marshal.PtrToStructure(
                                serviceConfigPtr,
                                typeof(Windows.Advapi32.QueryServiceConfig)
                        );
                        serviceInfo.StartType = ConvertFromWindows(serviceConfig.dwStartType);
                    }
                    else
                    {
                        var errorCode = Marshal.GetLastWin32Error();
                        serviceInfo.ErrorCode = errorCode;
                        serviceInfo.ErrorMessage = "Can not query Windows service \"" + serviceName + "\" config, error code: " + errorCode;
                    }
                }
                catch (Exception e)
                {
                    Log.Error("Can not query Windows service \"" + serviceName + "\" start type: " + e.Message);
                }
                finally
                {
                    Marshal.FreeHGlobal(serviceConfigPtr);
                }

                serviceInfo = UpdateCurrentStateInWindows(serviceHandle, serviceInfo);

                Windows.Advapi32.CloseServiceHandle(serviceHandle);
            }

            Windows.Advapi32.CloseServiceHandle(managerHandle);
            return serviceInfo;
        }

        private static ServiceInfo StartInWindows(string serviceName)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
            {
                return new ServiceInfo
                {
                    ServiceName = serviceName,
                    ErrorCode = Windows.ERROR_INVALID_NAME,
                    ErrorMessage = "Service name \"" + serviceName + "\" is invalid"
                };
            }

            var managerHandle = Windows.Advapi32.OpenSCManagerW(
                null,
                null,
                Windows.Advapi32.ServiceControlManagerAccessRight.Connect
            );
            if (managerHandle == IntPtr.Zero)
            {
                var errorCode = Marshal.GetLastWin32Error();
                return new ServiceInfo
                {
                    ServiceName = serviceName,
                    ErrorCode = errorCode,
                    ErrorMessage = "Can not open Windows service controller manager, error code: " + errorCode
                };
            }

            var serviceInfo = new ServiceInfo
            {
                ServiceName = serviceName
            };
            var serviceHandle = Windows.Advapi32.OpenServiceW(
                    managerHandle,
                    serviceName,
                    Windows.Advapi32.ServiceAccessRight.Start | Windows.Advapi32.ServiceAccessRight.QueryStatus
            );
            if (serviceHandle == IntPtr.Zero)
            {
                var errorCode = Marshal.GetLastWin32Error();
                serviceInfo.ErrorCode = errorCode;
                serviceInfo.ErrorMessage = "Can not open Windows service \"" + serviceName + "\", error code: " + errorCode;
            }
            else
            {
                var success = Windows.Advapi32.StartServiceW(
                        serviceHandle,
                        0,
                        null
                );
                if (success)
                {
                    serviceInfo.CurrentState = CurrentState.Running;
                }
                else
                {
                    var errorCode = Marshal.GetLastWin32Error();
                    serviceInfo.ErrorCode = errorCode;
                    serviceInfo.ErrorMessage = "Can not start Windows service \"" + serviceName + "\", error code: " + errorCode;
                }

                serviceInfo = UpdateCurrentStateInWindows(serviceHandle, serviceInfo);

                Windows.Advapi32.CloseServiceHandle(serviceHandle);
            }

            Windows.Advapi32.CloseServiceHandle(managerHandle);
            return serviceInfo;
        }

        private static ServiceInfo UpdateCurrentStateInWindows(
                IntPtr serviceHandle,
                ServiceInfo serviceInfo)
        {
            if (serviceHandle == IntPtr.Zero || serviceInfo == null)
            {
                return serviceInfo;
            }

            var status = new Windows.Advapi32.ServiceStatus();
            var success = Windows.Advapi32.QueryServiceStatus(
                    serviceHandle,
                    ref status
            );
            if (success)
            {
                serviceInfo.CurrentState = ConvertFromWindows(status.dwCurrentState);
            }
            else if (serviceInfo.ErrorCode != 0)
            {
                var errorCode = Marshal.GetLastWin32Error();
                serviceInfo.ErrorCode = errorCode;
                serviceInfo.ErrorMessage = "Can not query Windows service \"" + serviceInfo.ServiceName + "\" status, error code: " + errorCode;
            }

            return serviceInfo;
        }

        private static Windows.Advapi32.StartType ConvertToWindows(StartType startType)
        {
            if (startType == StartType.Disabled)
            {
                return Windows.Advapi32.StartType.Disabled;
            }
            if (startType == StartType.Manual)
            {
                return Windows.Advapi32.StartType.DemandStart;
            }
            if (startType == StartType.Automatic)
            {
                return Windows.Advapi32.StartType.AutoStart;
            }
            Log.Error("Can not convert service start type " + startType + " in Windows. Use Windows.Advapi32.StartType.AutoStart as fallback type");
            return Windows.Advapi32.StartType.AutoStart;
        }

        private static StartType ConvertFromWindows(Windows.Advapi32.StartType startType)
        {
            if (startType == Windows.Advapi32.StartType.AutoStart)
            {
                return StartType.Automatic;
            }
            if (startType == Windows.Advapi32.StartType.DemandStart)
            {
                return StartType.Manual;
            }
            if (startType == Windows.Advapi32.StartType.Disabled)
            {
                return StartType.Disabled;
            }
            Log.Error("Can not convert Windows service start type " + startType + ". Use Automatic as fallback type");
            return StartType.Automatic;
        }

        private static CurrentState ConvertFromWindows(Windows.Advapi32.CURRENT_STATE currentState)
        {
            if (currentState == Windows.Advapi32.CURRENT_STATE.SERVICE_CONTINUE_PENDING)
            {
                return CurrentState.ContinuePending;
            }
            if (currentState == Windows.Advapi32.CURRENT_STATE.SERVICE_PAUSED)
            {
                return CurrentState.Paused;
            }
            if (currentState == Windows.Advapi32.CURRENT_STATE.SERVICE_PAUSE_PENDING)
            {
                return CurrentState.PausePending;
            }
            if (currentState == Windows.Advapi32.CURRENT_STATE.SERVICE_RUNNING)
            {
                return CurrentState.Running;
            }
            if (currentState == Windows.Advapi32.CURRENT_STATE.SERVICE_START_PENDING)
            {
                return CurrentState.StartPending;
            }
            if (currentState == Windows.Advapi32.CURRENT_STATE.SERVICE_STOPPED)
            {
                return CurrentState.Stopped;
            }
            if (currentState == Windows.Advapi32.CURRENT_STATE.SERVICE_STOP_PENDING)
            {
                return CurrentState.StopPending;
            }
            Log.Error("Can not convert Windows service current state " + currentState + ". Use Unknown as fallback state");
            return CurrentState.Unknown;
        }

        public enum StartType
        {
            Unknown = 0,
            NotAvailable = 1,
            Disabled = 2,
            Manual = 3,
            Automatic = 4,
            DelayedAutomatic = 5
        }

        public enum CurrentState
        {
            Unknown = 0,
            NotAvailable = 1,
            Stopped = 2,
            StartPending = 3,
            StopPending = 4,
            Running = 5,
            ContinuePending = 6,
            PausePending = 7,
            Paused = 8
        }
    }

    public class ServiceInfo
    {
        public string ServiceName { get; set; }
        public ServiceManager.CurrentState CurrentState { get; set; } = ServiceManager.CurrentState.Unknown;
        public ServiceManager.StartType StartType { get; set; } = ServiceManager.StartType.Unknown;
        public int ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
    }
}
