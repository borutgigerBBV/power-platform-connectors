public class Script : ScriptBase
{

    public override async Task<HttpResponseMessage> ExecuteAsync()
    {
        HttpResponseMessage response = null;
        
        try
        {            
            // Check if the operation ID matches what is specified in the OpenAPI definition of the connector
            if (String.Equals(this.Context.OperationId, "GetLastInvoice", StringComparison.OrdinalIgnoreCase))
            {
                this.Context.Request.RequestUri = ReplaceUri(this.Context.Request.RequestUri, "GetLastInvoice", "ListInvoices");
                response = await this.HandleGetLastInvoiceOperation().ConfigureAwait(false); //response = await this.Context.SendAsync(this.Context.Request, this.CancellationToken);
                var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (response.Headers.Contains("apicallsleft"))
                {
                    IEnumerable<string> values;
                    if (response.Headers.TryGetValues("apicallsleft", out values))
                    {
                        string apicallsleft = values.First();

                        var result = JObject.Parse(responseString);
                        var newResult = new JObject
                        {                            
                            ["apicallsleft"] = apicallsleft,
                            ["data"] = result,  
                        };
                        
                        response.Content = CreateJsonContent(newResult.ToString());
                    }
                }            
            }
            else
            {
                //pass-thru any other operation to the API directly
                response = await this.HandleForwardOperation().ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {            
            response = new HttpResponseMessage(HttpStatusCode.BadRequest);
            response.Content = CreateJsonContent($"Error message: {ex.Message}");
        }
        
        return response;
    }

    private Uri ReplaceUri(Uri original, string fromValue, string toValue)
    {
        try
        {
            var builder = new UriBuilder(original.ToString().Replace(fromValue, toValue));
            return builder.Uri;
        }
        catch (Exception ex)
        {
            throw new Exception(original.ToString().Replace(fromValue, toValue));
        }
    }

    private async Task<HttpResponseMessage> HandleGetLastInvoiceOperation()
    {
        JObject newResult = null;
        // Use the context to send an HTTP request
        HttpResponseMessage response = await this.Context.SendAsync(this.Context.Request, this.CancellationToken).ConfigureAwait(continueOnCapturedContext: false);
        // Do the transformation if the response was successful, otherwise return error responses as-is
        if (response.IsSuccessStatusCode)
        {
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(continueOnCapturedContext: false);
                var result = JObject.Parse(responseString);
                // Wrap the original JSON object into a new JSON object with just two properties
                if (result != null && result.ContainsKey("invoices") && result["invoices"].HasValues)
                {
                    var sortedArray = result["invoices"].OrderBy(jo => (DateTime)jo["date"]).ToArray();
                    var lastInvoice = sortedArray[0];
                    newResult = new JObject
                    {
                        ["invoiceid"]   = lastInvoice["invoiceid"],
                        ["date"]        = lastInvoice["date"],
                        ["createDate"]  = lastInvoice["createDate"],
                        ["amount"]      = lastInvoice["amount"],
                        ["accountId"]   = lastInvoice["accountId"],
                        ["accountName"] = lastInvoice["accountName"],
                        ["status"]      = lastInvoice["status"],
                        ["typeId"]      = lastInvoice["typeId"],
                        ["purchaseOrderId"] = lastInvoice["purchaseOrderId"],
                        ["tags"]        = lastInvoice["tags"]

                    };
                }
                else
                {
                    newResult = new JObject
                    {
                        ["invoiceid"] = "-9999",
                        ["status"] = "No Invoices",
                    };
                }
            }
            else
            {
                newResult = new JObject
                    {
                        ["invoiceid"] = "-9999",
                        ["status"] = "Error retrieving invoices",
                    };
            }
            response.Content = CreateJsonContent(newResult.ToString());
            response.StatusCode = HttpStatusCode.OK;
        }
        return response;
    }

    private async Task<HttpResponseMessage> HandleForwardOperation()
    {
        // Use the context to forward/send an HTTP request
        HttpResponseMessage response = await this.Context.SendAsync(this.Context.Request, this.CancellationToken).ConfigureAwait(continueOnCapturedContext: false);
        return response;
    }
}