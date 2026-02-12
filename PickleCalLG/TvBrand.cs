using System;

namespace PickleCalLG
{
    /// <summary>
    /// Supported TV brands. Each brand has different levels of remote control capability.
    /// </summary>
    public enum TvBrand
    {
        /// <summary>No specific TV brand — manual calibration only.</summary>
        Other = 0,

        /// <summary>LG webOS TVs — full automated calibration support via SSAP WebSocket API.</summary>
        LG = 1,

        /// <summary>Samsung TVs — automated calibration support via SmartThings integration.</summary>
        Samsung = 2,

        /// <summary>Sony Bravia TVs — automated calibration support via Bravia Professional Calibration API.</summary>
        Sony = 3,

        /// <summary>TCL TVs — automated calibration support via TCL calibration API.</summary>
        TCL = 4,

        /// <summary>Hisense TVs — measurement only.</summary>
        Hisense = 5,

        /// <summary>Vizio TVs — measurement only.</summary>
        Vizio = 6,

        /// <summary>Panasonic TVs — automated calibration support via Panasonic calibration API.</summary>
        Panasonic = 7
    }

    /// <summary>
    /// Describes the calibration capabilities for a given TV brand.
    /// </summary>
    public sealed class TvBrandCapabilities
    {
        /// <summary>The brand this capability set describes.</summary>
        public TvBrand Brand { get; }

        /// <summary>Human-readable display name.</summary>
        public string DisplayName { get; }

        /// <summary>Whether we can connect and control this brand remotely.</summary>
        public bool SupportsRemoteConnection { get; }

        /// <summary>Whether this brand supports automated white balance adjustment.</summary>
        public bool SupportsAutoCalWhiteBalance { get; }

        /// <summary>Whether this brand supports automated CMS (color management) adjustment.</summary>
        public bool SupportsAutoCalCms { get; }

        /// <summary>Whether this brand supports reading current picture settings.</summary>
        public bool SupportsReadSettings { get; }

        /// <summary>Whether we can auto-apply recommended picture mode/settings.</summary>
        public bool SupportsAutoApplySettings { get; }

        /// <summary>Description of what's supported for this brand.</summary>
        public string Description { get; }

        private TvBrandCapabilities(TvBrand brand, string displayName,
            bool remoteConnection, bool autoCalWb, bool autoCalCms,
            bool readSettings, bool autoApplySettings, string description)
        {
            Brand = brand;
            DisplayName = displayName;
            SupportsRemoteConnection = remoteConnection;
            SupportsAutoCalWhiteBalance = autoCalWb;
            SupportsAutoCalCms = autoCalCms;
            SupportsReadSettings = readSettings;
            SupportsAutoApplySettings = autoApplySettings;
            Description = description;
        }

        /// <summary>Whether any form of automated calibration is supported.</summary>
        public bool SupportsAutoCal => SupportsAutoCalWhiteBalance || SupportsAutoCalCms;

        /// <summary>Get capabilities for a given brand.</summary>
        public static TvBrandCapabilities For(TvBrand brand) => brand switch
        {
            TvBrand.LG => new TvBrandCapabilities(
                TvBrand.LG, "LG",
                remoteConnection: true,
                autoCalWb: true,
                autoCalCms: true,
                readSettings: true,
                autoApplySettings: true,
                "Full support — remote connection, automated white balance + CMS, picture settings control"),

            TvBrand.Samsung => new TvBrandCapabilities(
                TvBrand.Samsung, "Samsung",
                remoteConnection: true,
                autoCalWb: true,
                autoCalCms: true,
                readSettings: false,
                autoApplySettings: false,
                "Automated calibration — white balance + CMS via SmartThings calibration API"),

            TvBrand.Sony => new TvBrandCapabilities(
                TvBrand.Sony, "Sony",
                remoteConnection: true,
                autoCalWb: true,
                autoCalCms: true,
                readSettings: false,
                autoApplySettings: false,
                "Automated calibration — white balance + CMS via Bravia Professional Calibration API"),

            TvBrand.TCL => new TvBrandCapabilities(
                TvBrand.TCL, "TCL",
                remoteConnection: true,
                autoCalWb: true,
                autoCalCms: true,
                readSettings: false,
                autoApplySettings: false,
                "Automated calibration — white balance + CMS via TCL calibration API (3D LUT)"),

            TvBrand.Panasonic => new TvBrandCapabilities(
                TvBrand.Panasonic, "Panasonic",
                remoteConnection: true,
                autoCalWb: true,
                autoCalCms: true,
                readSettings: false,
                autoApplySettings: false,
                "Automated calibration — white balance + CMS via Panasonic calibration API"),

            TvBrand.Hisense => new TvBrandCapabilities(
                TvBrand.Hisense, "Hisense",
                remoteConnection: false,
                autoCalWb: false,
                autoCalCms: false,
                readSettings: false,
                autoApplySettings: false,
                "Measurement and analysis only — manual TV adjustments required"),

            TvBrand.Vizio => new TvBrandCapabilities(
                TvBrand.Vizio, "Vizio",
                remoteConnection: false,
                autoCalWb: false,
                autoCalCms: false,
                readSettings: false,
                autoApplySettings: false,
                "Measurement and analysis only — manual TV adjustments required"),

            _ => new TvBrandCapabilities(
                TvBrand.Other, "Other / Generic",
                remoteConnection: false,
                autoCalWb: false,
                autoCalCms: false,
                readSettings: false,
                autoApplySettings: false,
                "Measurement and analysis only — manual TV adjustments required")
        };

        /// <summary>All known brands for UI population.</summary>
        public static TvBrand[] AllBrands => new[]
        {
            TvBrand.LG,
            TvBrand.Samsung,
            TvBrand.Sony,
            TvBrand.Panasonic,
            TvBrand.TCL,
            TvBrand.Hisense,
            TvBrand.Vizio,
            TvBrand.Other
        };

        public override string ToString() => DisplayName;
    }
}
