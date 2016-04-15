﻿namespace Microsoft.HockeyApp.Extensibility.Windows
{
    using System;
    using System.Collections.Generic;
    using Channel;
    using DataContracts;
    using Extensibility.Implementation;
    using Extensibility.Implementation.Platform;
    using global::Windows.ApplicationModel;

    /// <summary>
    /// Tracks anonymous user Id for Store Apps (Windows Store and Windows Phone).
    /// </summary>
    internal sealed class UserContextInitializer : ITelemetryInitializer
    {
        private const string UserIdSetting = "HockeyAppUserId";
        private const string UserAcquisitionDateSetting = "HockeyAppUserAcquisitionDate";
        private static object syncRoot = new object();

        private string userId;
        private DateTimeOffset? userAcquisitionDate;

        /// <summary>
        /// Initializes <see cref="UserContext.Id"/> property of the <see cref="TelemetryContext.User"/> telemetry
        /// and updates the IsFirst property of the SessionContext.
        /// </summary>
        public void Initialize(ITelemetry telemetry)
        {
            this.InitializeUserData();
            telemetry.Context.User.Id = this.userId;
            telemetry.Context.User.AcquisitionDate = this.userAcquisitionDate;
            telemetry.Context.User.StoreRegion = UserContextReader.GetStoreRegion();
        }

        private void InitializeUserData()
        {
            if (this.userId != null)
            {
                return;
            }

            lock (syncRoot)
            {
                if (this.userId != null)
                {
                    return;
                }

#if WINDOWS_UWP
                if (Package.Current.IsDevelopmentMode)
                {
                    // There is a known issue with ApplicationData.Current.RoamingSettings in emulator mode:
                    // Windows Phone emulator does not save its state, so every time users starts an emulator, we will get a new fresh system as if user turned on a real phone for the first time.
                    // This can cause to generate new user id on every new emulator start, which is an incorrect experience.
                    // Therefore we check if pacakge is deployment in DevelopmentMode (meaning not from the store) and if it is - we assume that it is running in emulator and 
                    // set in this case user guid to an empty value.
                    this.userId = new Guid().ToString(); // make "empty" all-0 guid 
                    return;
                }
#endif

                IDictionary<string, object> settings = PlatformSingleton.Current.GetRoamingApplicationSettings();

                object storedUserAcquisitionDate;
                if (settings.TryGetValue(UserAcquisitionDateSetting, out storedUserAcquisitionDate))
                {
                    this.userAcquisitionDate = (DateTimeOffset)storedUserAcquisitionDate;
                }
                else
                {
                    this.userAcquisitionDate = DateTimeOffset.UtcNow;
                    settings[UserAcquisitionDateSetting] = this.userAcquisitionDate;
                }

                object storedUserId;
                if (settings.TryGetValue(UserIdSetting, out storedUserId))
                {
                    this.userId = (string)storedUserId;
                    return;
                }

                this.userId = Guid.NewGuid().ToString();
                settings[UserIdSetting] = this.userId;
            }
        }
    }
}