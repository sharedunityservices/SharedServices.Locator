using System;
using System.Collections.Generic;
using SharedServices.V1;

namespace SharedServices.Locator.V1
{
    public interface IOverrideServices
    {
        void OverrideServices(Dictionary<Type, IService> services);
    }
}