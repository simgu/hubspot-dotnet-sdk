using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HubSpot.Model;
using HubSpot.Model.Companies;
using HubSpot.Utils;

namespace HubSpot
{
    public partial class HttpHubSpotClient : IHubSpotCompanyClient
    {
        async Task<Company> IHubSpotCompanyClient.GetByIdAsync(long companyId)
        {
            var result = await SendAsync<Company>(HttpMethod.Get, $"/companies/v2/companies/{companyId}");
            return result;
        }

        async Task<Company> IHubSpotCompanyClient.CreateAsync(IReadOnlyList<ValuedProperty> properties)
        {
            if (properties == null)
            {
                throw new ArgumentNullException(nameof(properties));
            }

            var list = new PropertyList {Properties = properties};
            var result = await SendAsync<PropertyList, Company>(HttpMethod.Post, list, "/companies/v2/companies/");

            return result;
        }

        async Task<Company> IHubSpotCompanyClient.UpdateAsync(long companyId, IReadOnlyList<ValuedProperty> propertiesToUpdate)
        {
            if (propertiesToUpdate == null)
            {
                throw new ArgumentNullException(nameof(propertiesToUpdate));
            }

            var list = new PropertyList { Properties = propertiesToUpdate };
            var result = await SendAsync<PropertyList, Company>(HttpMethod.Put, list, $"/companies/v2/companies/{companyId}");

            return result;
        }

        async Task IHubSpotCompanyClient.UpdateManyAsync(IReadOnlyList<ObjectPropertyList> companiesToUpdate)
        {
            if (companiesToUpdate == null)
            {
                throw new ArgumentNullException(nameof(companiesToUpdate));
            }

            await SendAsync(HttpMethod.Post, companiesToUpdate, "/companies/v1/batch-async/update");
        }

        async Task<DeleteCompanyResponse> IHubSpotCompanyClient.DeleteAsync(long companyId)
        {
            var result = await SendAsync<DeleteCompanyResponse>(HttpMethod.Delete, $"/companies/v2/companies/{companyId}");
            return result;
        }

        async Task<CompanyList> IHubSpotCompanyClient.GetAllAsync(IReadOnlyList<IProperty> properties, IReadOnlyList<IProperty> propertiesWithHistory, int limit = 100, long? companyOffset = null)
        {
            var builder = new HttpQueryStringBuilder();
            builder.AddProperties(properties, "properties");
            builder.AddProperties(propertiesWithHistory, "propertiesWithHistory");
            builder.Add("limit", limit.ToString());

            if (companyOffset.HasValue)
                builder.Add("offset", companyOffset.Value.ToString());

            var result = await SendAsync<CompanyList>(HttpMethod.Get, "/companies/v2/companies/paged", builder.BuildQuery());

            return result;
        }

        async Task<PagedList<Company>> IHubSpotCompanyClient.GetRecentlyCreatedAsync(int count = 100, long? offset = null)
        {
            var builder = new HttpQueryStringBuilder();
            builder.Add("count", count.ToString());

            if (offset.HasValue)
                builder.Add("offset", offset.Value.ToString());

            var result = await SendAsync<PagedList<Company>>(HttpMethod.Get, "/companies/v2/companies/recent/created", builder.BuildQuery());

            return result;
        }

        async Task<PagedList<Company>> IHubSpotCompanyClient.GetRecentlyUpdatedAsync(int count = 100, long? offset = null)
        {
            var builder = new HttpQueryStringBuilder();
            builder.Add("count", count.ToString());

            if (offset.HasValue)
                builder.Add("offset", offset.Value.ToString());

            var result = await SendAsync<PagedList<Company>>(HttpMethod.Get, "/companies/v2/companies/recent/modified", builder.BuildQuery());

            return result;
        }

        async Task<SearchResponse> IHubSpotCompanyClient.SearchAsync(string domain, IReadOnlyList<IProperty> properties = null, int limit = 100, long? companyOffset = null)
        {
            if (string.IsNullOrEmpty(domain))
            {
                throw new ArgumentNullException(nameof(domain));
            }

            var request = new
            {
                limit,
                requestOptions = new
                {
                    properties = properties.Select(p => p.Name).ToArray()
                },
                offset = new SearchResponse.SearchResponseOffset
                {
                    IsPrimary = true,
                    CompanyId = companyOffset ?? 0
                }
            };

            var response = await SendAsync<object, SearchResponse>(HttpMethod.Post, request, $"/companies/v2/domains/{domain}/companies");

            return response;
        }

        async Task<ContactList> IHubSpotCompanyClient.GetContactsInCompanyAsync(long companyId, int count = 100, long? companyOffset = null)
        {
            var builder = new HttpQueryStringBuilder();
            builder.Add("count", count.ToString());

            if (companyOffset.HasValue)
                builder.Add("vidOffset", companyOffset.Value.ToString());

            var result = await SendAsync<ContactList>(HttpMethod.Get, $"/companies/v2/companies/{companyId}/contacts", builder.BuildQuery());

            return result;
        }

        async Task<ContactIdList> IHubSpotCompanyClient.GetContactIdsInCompanyAsync(long companyId, int count = 100, long? companyOffset = null)
        {
            var builder = new HttpQueryStringBuilder();
            builder.Add("count", count.ToString());

            if (companyOffset.HasValue)
                builder.Add("vidOffset", companyOffset.Value.ToString());

            var result = await SendAsync<ContactIdList>(HttpMethod.Get, $"/companies/v2/companies/{companyId}/vids", builder.BuildQuery());

            return result;
        }

        async Task<Company> IHubSpotCompanyClient.AddContactToCompanyAsync(long companyId, long contactId)
        {
            var result = await SendAsync<Company>(HttpMethod.Put, $"/companies/v2/companies/{companyId}/contacts/{contactId}");

            return result;
        }

        async Task IHubSpotCompanyClient.RemoveContactFromCompanyAsync(long companyId, long contactId)
        {
            await SendAsync<Company>(HttpMethod.Delete, $"/companies/v2/companies/{companyId}/contacts/{contactId}");
        }
    }
}