using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Hondarersoft.WebInterface
{
    public interface ICommonApiService
    {
        public Task<ICommonApiService> StartAsync();

        public ICommonApiService RegistController(IConfiguration configurationRoot);

        public ICommonApiService RegistInterface(IWebInterface webInterfaceBase, string identify = null);

        public Task<CommonApiResponse> SendRequestAsync(CommonApiRequest request, string interfaceIdentify = null, string sessionIdentify = null);

        public Task<CommonApiResponse> SendRequestAsync<T>(CommonApiRequest request, string interfaceIdentify = null, string sessionIdentify = null);
    }
}
