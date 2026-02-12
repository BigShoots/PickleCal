namespace PickleCalLG
{
    /// <summary>
    /// Creates the correct <see cref="ITvController"/> for a given TV brand.
    /// </summary>
    public static class TvControllerFactory
    {
        /// <summary>
        /// Create a brand-specific TV controller.
        /// </summary>
        /// <param name="brand">The TV brand.</param>
        /// <param name="tvIp">The TV's IP address.</param>
        /// <param name="useSecure">For LG: whether to use wss:// (default true).</param>
        public static ITvController Create(TvBrand brand, string tvIp, bool useSecure = true)
        {
            return brand switch
            {
                TvBrand.LG       => new LgTvController(tvIp, useSecure),
                TvBrand.Samsung  => new SamsungTvController(tvIp),
                TvBrand.Sony     => new SonyTvController(tvIp),
                TvBrand.Panasonic => new PanasonicTvController(tvIp),
                TvBrand.TCL      => new TclTvController(tvIp),
                _                => new GenericTvController(brand, tvIp)
            };
        }
    }
}
