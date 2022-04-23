using System;
using System.Collections.Generic;
using System.Text;

namespace PointApp.Utilities
{
    public static class DeviceUtility
    {
        /// <summary>
        /// 端末のネットワークへの通信状況を取得
        /// </summary>
        public  static bool IsNetworkConnect()
        {
            return Xamarin.Essentials.Connectivity.NetworkAccess == Xamarin.Essentials.NetworkAccess.Internet;
        }
    }
}
