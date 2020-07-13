using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SimpleThermostat
{
  class Dps
  {
    internal static async Task<DeviceClient> ProvisionDeviceWithSasKeyAsync(string scopeId, string deviceId, string deviceKey, string modelId, ILogger log)
    {
      using (var transport = new ProvisioningTransportHandlerMqtt())
      {
        using (var security = new SecurityProviderSymmetricKey(deviceId, deviceKey, null))
        {
          DeviceRegistrationResult provResult;
          var provClient = ProvisioningDeviceClient.Create("global.azure-devices-provisioning.net", scopeId, security, transport);

          if (!string.IsNullOrEmpty(modelId))
          {
            provResult = await provClient.RegisterAsync(GetProvisionPayload(modelId)).ConfigureAwait(false);
          }
          else
          {
            provResult = await provClient.RegisterAsync().ConfigureAwait(false);
          }

          log.LogInformation($"Provioning Result. Status [{provResult.Status}] SubStatus [{provResult.Substatus}]");

          if (provResult.Status == ProvisioningRegistrationStatusType.Assigned)
          {
            log.LogWarning($"Device {provResult.DeviceId} in Hub {provResult.AssignedHub}");
            log.LogInformation($"LastRefresh {provResult.LastUpdatedDateTimeUtc} RegistrationId {provResult.RegistrationId}");
            var csBuilder = IotHubConnectionStringBuilder.Create(provResult.AssignedHub, new DeviceAuthenticationWithRegistrySymmetricKey(provResult.DeviceId, security.GetPrimaryKey()));
            string connectionString = csBuilder.ToString();
            return await Task.FromResult(
              DeviceClient.CreateFromConnectionString(
                connectionString, TransportType.Mqtt,
                  new ClientOptions() { ModelId = modelId }));
          }
          else
          {
            string errorMessage = $"Device not provisioned. Message: {provResult.ErrorMessage}";
            log.LogError(errorMessage);
            throw new IotHubException(errorMessage);
          }
        }
      }
    }

    static ProvisioningRegistrationAdditionalData GetProvisionPayload(string modelId)
    {
      return new ProvisioningRegistrationAdditionalData
      {
        JsonData = "{ iotcModelId: '" + modelId + "'}"
      };
    }
  }
}
