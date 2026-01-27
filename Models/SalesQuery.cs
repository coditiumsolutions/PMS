using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PMS.Web.Models
{
    public class SalesApiResponse
    {
        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("data")]
        public List<SalesQuery> Data { get; set; }
    }

    public class SalesQuery
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("query_type")]
        public string QueryType { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("created_by")]
        public object CreatedBy { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("mobile_no")]
        public string MobileNo { get; set; }

        [JsonPropertyName("address")]
        public string Address { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("responder_remarks")]
        public string ResponderRemarks { get; set; }

        [JsonPropertyName("comments")]
        public string Comments { get; set; }

        [JsonPropertyName("created_at")]
        public string CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public string UpdatedAt { get; set; }

        [JsonPropertyName("updated_by")]
        public object UpdatedBy { get; set; }
    }

    public class SalesUpdateResponse
    {
        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }
    }
}
