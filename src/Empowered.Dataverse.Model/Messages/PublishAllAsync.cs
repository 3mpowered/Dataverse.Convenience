#pragma warning disable CS1591
//------------------------------------------------------------------------------
// <auto-generated>
//     Dieser Code wurde von einem Tool generiert.
//     Laufzeitversion:4.0.30319.42000
//
//     Änderungen an dieser Datei können falsches Verhalten verursachen und gehen verloren, wenn
//     der Code erneut generiert wird.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Empowered.Dataverse.Model
{
	
	
	[System.Runtime.Serialization.DataContractAttribute(Namespace="http://schemas.microsoft.com/crm/2011/Contracts")]
	[Microsoft.Xrm.Sdk.Client.RequestProxyAttribute("PublishAllXmlAsync")]
	[System.CodeDom.Compiler.GeneratedCodeAttribute("Dataverse Model Builder", "2.0.0.11")]
	public partial class PublishAllXmlAsyncRequest : Microsoft.Xrm.Sdk.OrganizationRequest
	{
		
		public PublishAllXmlAsyncRequest()
		{
			this.RequestName = "PublishAllXmlAsync";
		}
	}
	
	[System.Runtime.Serialization.DataContractAttribute(Namespace="http://schemas.microsoft.com/crm/2011/Contracts")]
	[Microsoft.Xrm.Sdk.Client.ResponseProxyAttribute("PublishAllXmlAsync")]
	[System.CodeDom.Compiler.GeneratedCodeAttribute("Dataverse Model Builder", "2.0.0.11")]
	public partial class PublishAllXmlAsyncResponse : Microsoft.Xrm.Sdk.OrganizationResponse
	{
		
		public PublishAllXmlAsyncResponse()
		{
		}
		
		public System.Guid AsyncOperationId
		{
			get
			{
				if (this.Results.Contains("AsyncOperationId"))
				{
					return ((System.Guid)(this.Results["AsyncOperationId"]));
				}
				else
				{
					return default(System.Guid);
				}
			}
		}
	}
}
#pragma warning restore CS1591