﻿using CommerceHub_OrderManager.channel.sears;
using System;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace CommerceHub_OrderManager.channel.brightpearl
{
    /* 
     * A class that post order request to brightpearl
     */
    public class BPconnect
    {
        // fields for brightpearl integration
        private GetRequest get;
        private PostRequest post;

        /* constructor that initialize request objects*/
        public BPconnect()
        {
            // initialize API authentication
            SqlConnection authenticationConnection = new SqlConnection(Properties.Settings.Default.ASCMcs);
            SqlCommand getAuthetication = new SqlCommand("SELECT Field3_Value, Field1_Value FROM ASCM_Credentials WHERE Source = \'Brightpearl Testing\';", authenticationConnection);
            authenticationConnection.Open();
            SqlDataReader reader = getAuthetication.ExecuteReader();
            reader.Read();
            string appRef = reader.GetString(0);
            string appToken = reader.GetString(1);
            authenticationConnection.Close();

            // initializes request fields
            get = new GetRequest(appRef, appToken);
            post = new PostRequest(appRef, appToken);
        }

        /* a method that post sears order to brightpearl */
        public void postOrder(SearsValues value, int[] cancelList)
        {
            // check if the order is cancelled entirely -> if it is just return no need to post it
            if (cancelList.Length >= value.LineCount)
                return;

            // get contact id first
            string name = value.Recipient.Name;
            string contactId = get.getCustomerId(name.Remove(name.IndexOf(' ')), name.Substring(name.IndexOf(' ') + 1));

            // field for receipt
            double total = value.TrxBalanceDue;

            // if customer exists, add the current order under this customer
            if (contactId != null)
            {
                #region Cusomter Exist Case
                // initialize order BPvalues object
                BPvalues orderValue = new BPvalues(null, value.TransactionID, value.CustOrderDate, 7, 7, null, null, 0, 0, 0, 0);

                // post order
                string orderId = post.postOrderRequest(contactId, orderValue);
                if (orderId == "Error")
                {
                    do
                    {
                        Thread.Sleep(5000);
                        orderId = post.postOrderRequest(contactId, orderValue);
                    } while (orderId == "Error");
                }

                // post order row and reservation
                for (int i = 0; i < value.LineCount; i++)
                {
                    // boolean flag to see if the item is cancelled
                    bool cancelled = false;

                    // check if the item is cancelled or not
                    foreach (int j in cancelList)
                    {
                        if (j == i)
                        {
                            // substract the item's price
                            total -= value.LineBalanceDue[j];

                            cancelled = true;
                            break;
                        }
                    }

                    // the case if not cancel post it to brightpearl
                    if (!cancelled)
                    {
                        // GST, HST, PST
                        double tax = value.GST_HST_Extended[i] + value.PST_Extended[i] + value.GST_HST_Total[i] + value.PST_Total[i];

                        // initialize item BPvalues object
                        BPvalues itemValue = new BPvalues(null, null, DateTime.Today, 7, 7, value.TrxVendorSKU[i], value.Description[i], value.TrxQty[i], value.UnitPrice[i], tax, value.LineBalanceDue[i]);

                        // post order row
                        string orderRowId = post.postOrderRowRequest(orderId, itemValue);
                        if (orderRowId == "Error")
                        {
                            do
                            {
                                Thread.Sleep(5000);
                                orderRowId = post.postOrderRowRequest(orderId, itemValue);
                            } while (orderRowId == "Error");
                        }

                        // post reservation
                        post.postReservationRequest(orderId, orderRowId, itemValue);
                        if (post.HasError)
                        {
                            do
                            {
                                Thread.Sleep(5000);
                                post.postReservationRequest(orderId, orderRowId, itemValue);
                            } while (post.HasError);
                        }
                    }
                }

                // set total paid to bp value
                orderValue.TotalPaid = total;

                // post receipt
                post.postReceipt(orderId, contactId, orderValue);
                if (post.HasError)
                {
                    do
                    {
                        Thread.Sleep(5000);
                        post.postReceipt(orderId, contactId, orderValue);
                    } while (post.HasError);
                }
                #endregion
            }
            else
            {
                #region Customer Not Exist Case
                // initialize order BPvalues object
                BPvalues orderValue = new BPvalues(value.Recipient, value.TransactionID, value.CustOrderDate, 7, 7, null, null, 0, 0, 0, 0);

                // post new order with new customer
                string addressId = post.postAddressRequest(orderValue.Address);
                contactId = post.postContactRequest(addressId, orderValue);

                // post order
                string orderId = post.postOrderRequest(contactId, orderValue);
                if (orderId == "Error")
                {
                    do
                    {
                        Thread.Sleep(5000);
                        orderId = post.postOrderRequest(contactId, orderValue);
                    } while (orderId == "Error");
                }

                // post order row and reservation
                for (int i = 0; i < value.LineCount; i++)
                {
                    // boolean flag to see if the item is cancelled
                    bool cancelled = false;

                    // check if the item is cancelled or not
                    foreach (int j in cancelList)
                    {
                        if (j == i)
                        {
                            // substract the item's price
                            total -= value.LineBalanceDue[j];

                            cancelled = true;
                            break;
                        }
                    }

                    // the case if not cancel post it to brightpearl
                    if (!cancelled)
                    {
                        // GST, HST, PST
                        double tax = value.GST_HST_Extended[i] + value.PST_Extended[i] + value.GST_HST_Total[i] + value.PST_Total[i];

                        // initialize item BPvalues object
                        BPvalues itemValue = new BPvalues(null, null, DateTime.Today, 7, 7, value.TrxVendorSKU[i], value.Description[i], value.TrxQty[i], value.UnitPrice[i], tax, value.LineBalanceDue[i]);

                        // post order row
                        string orderRowId = post.postOrderRowRequest(orderId, itemValue);
                        if (orderRowId == "Error")
                        {
                            do
                            {
                                Thread.Sleep(5000);
                                orderRowId = post.postOrderRowRequest(orderId, itemValue);
                            } while (orderRowId == "Error");
                        }

                        // post reservation
                        post.postReservationRequest(orderId, orderRowId, itemValue);
                        if (post.HasError)
                        {
                            do
                            {
                                Thread.Sleep(5000);
                                post.postReservationRequest(orderId, orderRowId, itemValue);
                            } while (post.HasError);
                        }
                    }
                }

                // set total paid to bp value
                orderValue.TotalPaid = total;

                // post receipt
                post.postReceipt(orderId, contactId, orderValue);
                if (post.HasError)
                {
                    do
                    {
                        Thread.Sleep(5000);
                        post.postReceipt(orderId, contactId, orderValue);
                    } while (post.HasError);
                }
                #endregion
            }
        }

        #region Supporting Methods
        /* a method that substring the given string */
        private static string substringMethod(string original, string startingString, int additionIndex)
        {
            string copy = original;
            copy = original.Substring(original.IndexOf(startingString) + additionIndex);

            return copy;
        }

        /* a method that get the next target token */
        private static string getTarget(string text)
        {
            int i = 0;
            while (text[i] != '"' && text[i] != ',' && text[i] != '}')
            {
                i++;
            }

            return text.Substring(0, i);
        }
        #endregion

        /* 
         * A class that Get request from brightpearl
         */
        private class GetRequest
        {
            // fields for web request
            private WebRequest request;
            private HttpWebResponse response;

            // fields for credentials
            private string appRef;
            private string appToken;

            /* constructor to initialize the web request of app reference and app token */
            public GetRequest(string appRef, string appToken)
            {
                this.appRef = appRef;
                this.appToken = appToken;
            }

            /* a method that return customer id from given firstname and lastname*/
            public string getCustomerId(string firstName, string lastName)
            {
                string uri = "https://ws-use.brightpearl.com/2.0.0/ashlintest/contact-service/contact-search?firstName=" + firstName + "&lastName=" + lastName;

                // post request to uri
                request = WebRequest.Create(uri);
                request.Headers.Add("brightpearl-app-ref", appRef);
                request.Headers.Add("brightpearl-account-token", appToken);
                request.Method = "GET";

                // get the response from the server
                response = (HttpWebResponse)request.GetResponse();

                // read all the text from JSON response
                string textJSON;
                using (StreamReader streamReader = new StreamReader(response.GetResponseStream()))
                {
                    textJSON = streamReader.ReadToEnd();
                }

                // check if there is result return or not
                textJSON = substringMethod(textJSON, "resultsReturned", 17);
                if (Convert.ToInt32(getTarget(textJSON)) < 1)
                    return null;

                // getting customer id
                textJSON = substringMethod(textJSON, "\"results\":", 12);

                return getTarget(textJSON);
            }

            /* a method that return product id from given sku */
            public string getProductId(string sku)
            {
                string uri = "https://ws-use.brightpearl.com/2.0.0/ashlintest/product-service/product-search?SKU=" + sku;

                // post request to uri
                request = WebRequest.Create(uri);
                request.Headers.Add("brightpearl-app-ref", appRef);
                request.Headers.Add("brightpearl-account-token", appToken);
                request.Method = "GET";

                // get the response from the server
                response = (HttpWebResponse)request.GetResponse();

                // read all the text from JSON response
                string textJSON;
                using (StreamReader streamReader = new StreamReader(response.GetResponseStream()))
                {
                    textJSON = streamReader.ReadToEnd();
                }

                // check if there is result return or not
                textJSON = substringMethod(textJSON, "resultsReturned", 17);
                if (Convert.ToInt32(getTarget(textJSON)) < 1)
                    return null;

                // getting product id
                textJSON = substringMethod(textJSON, "\"results\":", 12);

                return getTarget(textJSON);
            }
        }

        /* 
         * A class that Post request to brightpearl 
         */
        private class PostRequest
        {
            // fields for web request
            private HttpWebRequest request;
            private HttpWebResponse response;

            // fields for credentials
            private string appRef;
            private string appToken;

            // field for telling client if there is error occur
            public bool HasError { get; set; }

            /* constructor to initialize the web request of app reference and app token */
            public PostRequest(string appRef, string appToken)
            {
                this.appRef = appRef;
                this.appToken = appToken;

                HasError = false;
            }

            /* post new address to API */
            public string postAddressRequest(Address address)
            {
                string uri = "https://ws-use.brightpearl.com/2.0.0/ashlintest/contact-service/postal-address";

                request = (HttpWebRequest)WebRequest.Create(uri);
                request.Method = "POST";
                request.ContentType = "application/json";
                request.Headers.Add("brightpearl-app-ref", appRef);
                request.Headers.Add("brightpearl-account-token", appToken);

                // generate the JSON file for address post
                string textJSON = "{\"addressLine1\":\"" + address.Address1 + "\",\"addressLine2\":\"" + address.Address2 + "\",\"addressLine3\":\"" + address.City + "\",\"addressLine4\":\"" + address.State + "\",\"postalCode\":\"" + address.PostalCode + "\",\"countryIsoCode\":\"CAN\"}";

                // turn request string into a byte stream
                byte[] postBytes = Encoding.UTF8.GetBytes(textJSON);

                // send request
                using (Stream requestStream = request.GetRequestStream())
                {
                    requestStream.Write(postBytes, 0, postBytes.Length);
                }

                // get the response from the server
                response = (HttpWebResponse)request.GetResponse();
                string result;
                using (StreamReader streamReader = new StreamReader(response.GetResponseStream()))
                {
                    result = streamReader.ReadToEnd();
                }

                result = substringMethod(result, ":", 1);

                return getTarget(result);  //return the addresss ID
            }

            /* post new customer to API */
            public string postContactRequest(string addressID, BPvalues value)
            {
                string uri = "https://ws-use.brightpearl.com/2.0.0/ashlintest/contact-service/contact";

                request = (HttpWebRequest)WebRequest.Create(uri);
                request.Method = "POST";
                request.ContentType = "application/json";
                request.Headers.Add("brightpearl-app-ref", appRef);
                request.Headers.Add("brightpearl-account-token", appToken);

                // generate JSON file for contact post
                string textJSON = "{\"firstName\":\"" + value.Address.Name.Remove(value.Address.Name.IndexOf(' ')) + "\",\"lastName\":\"" + value.Address.Name.Substring(value.Address.Name.IndexOf(' ') + 1) + "\",\"postAddressIds\":{\"DEF\":" + addressID + ",\"BIL\":" + addressID + ",\"DEL\":" + addressID + "}," + 
                                  "\"communication\":{\"telephones\":{\"PRI\":\"" + value.Address.DayPhone + "\"}},\"relationshipToAccount\":{\"isSupplier\": false,\"isStaff\":false,\"isCustomer\":true},\"financialDetails\":{\"priceListId\": 5}}";

                // turn request string into a byte stream
                byte[] postBytes = Encoding.UTF8.GetBytes(textJSON);

                // send request
                using (Stream requestStream = request.GetRequestStream())
                {
                    requestStream.Write(postBytes, 0, postBytes.Length);
                }

                // get the response from the server
                response = (HttpWebResponse)request.GetResponse();
                string result;
                using (StreamReader streamReader = new StreamReader(response.GetResponseStream()))
                {
                    result = streamReader.ReadToEnd();
                }

                int index = result.IndexOf(":") + 1;
                int length = index;
                while (Char.IsNumber(result[length]))
                {
                    length++;
                }
                string contactID = result.Substring(index, length - index);

                return contactID;  //return the contact ID
            }

            /* post new order to API */
            public string postOrderRequest(string contactID, BPvalues value)
            {
                string uri = "https://ws-use.brightpearl.com/2.0.0/ashlintest/order-service/order";

                request = (HttpWebRequest)WebRequest.Create(uri);
                request.Method = "POST";
                request.ContentType = "application/json";
                request.Headers.Add("brightpearl-app-ref", appRef);
                request.Headers.Add("brightpearl-account-token", appToken);

                // generate JSON file for order post
                string textJSON = "{\"orderTypeCode\":\"SO\",\"reference\":\"" + value.Reference + "\",\"placeOn\":\"" + value.PlaceOn.ToString("yyyy-MM-dd") + "T00:00:00+00:00\",\"orderStatus\":{\"orderStatusId\":2}," + 
                                  "\"delivery\":{\"deliveryDate\":\"" + DateTime.Today.ToString("yyyy-MM-dd") + "T00:00:00+00:00\",\"shippingMethodId\":7},\"currency\":{\"orderCurrencyCode\":\"CAD\"},\"parties\":{\"customer\":{\"contactId\":" + contactID + "}},\"assignment\":{\"current\":{\"channelId\":" + value.ChannelId + "}}}";


                // turn request string into a byte stream
                byte[] postBytes = Encoding.UTF8.GetBytes(textJSON);

                // send request
                using (Stream requestStream = request.GetRequestStream())
                {
                    requestStream.Write(postBytes, 0, postBytes.Length);
                }

                // get the response from the server
                try    // might have server internal error, so do it in try and catch
                {
                    response = (HttpWebResponse)request.GetResponse();
                }
                catch    // HTTP response 500
                {
                    return "Error";    // cannot post order, return error instead
                }

                string result;
                using (StreamReader streamReader = new StreamReader(response.GetResponseStream()))
                {
                    result = streamReader.ReadToEnd();
                }

                result = substringMethod(result, ":", 1);

                return getTarget(result);  //return the order ID
            }

            /* post new order row to API */
            public string postOrderRowRequest(string orderID, BPvalues value)
            {
                // get product id
                GetRequest get = new GetRequest(appRef, appToken);
                string productId = get.getProductId(value.SKU);

                string uri = "https://ws-use.brightpearl.com/2.0.0/ashlintest/order-service/order/" + orderID + "/row";
                request = (HttpWebRequest)WebRequest.Create(uri);
                request.Method = "POST";
                request.ContentType = "application/json";
                request.Headers.Add("brightpearl-app-ref", appRef);
                request.Headers.Add("brightpearl-account-token", appToken);

                // generate JSON file for order row post
                string textJSON;
                if (productId != null)
                {
                    textJSON = "{\"productId\":\"" + productId + "\",\"quantity\":{\"magnitude\":\"" + value.Quantity + "\"},\"rowValue\":{\"taxCode\":\"T\",\"rowNet\":{\"value\":\"" + value.RowNet + "\"},\"rowTax\":{\"value\":\"" + value.RowTax + "\"}}}";
                }
                else
                {
                    textJSON = "{\"productName\":\"" + value.ProductName + " " + value.SKU  + "\",\"quantity\":{\"magnitude\":\"" + value.Quantity + "\"},\"rowValue\":{\"taxCode\":\"T\",\"rowNet\":{\"value\":\"" + value.RowNet + "\"},\"rowTax\":{\"value\":\"" + value.RowTax + "\"}}}";
                }


                // turn request string into a byte stream
                byte[] postBytes = Encoding.UTF8.GetBytes(textJSON);

                // send request
                using (Stream requestStream = request.GetRequestStream())
                {
                    requestStream.Write(postBytes, 0, postBytes.Length);
                }

                // get the response from server
                try
                {
                    response = (HttpWebResponse)request.GetResponse();
                }
                catch
                {
                    return "Error";     // 503 Server Unabailable
                }
                string result;
                using (StreamReader streamReader = new StreamReader(response.GetResponseStream()))
                {
                    result = streamReader.ReadToEnd();
                }

                result = substringMethod(result, ":", 1);

                return getTarget(result);  //return the order row ID
            }

            /* post reservation request to API */
            public void postReservationRequest(string orderID, string orderRowID, BPvalues value)
            {
                // get product id
                GetRequest get = new GetRequest(appRef, appToken);
                string productId = get.getProductId(value.SKU);

                string uri = "https://ws-use.brightpearl.com/2.0.0/ashlintest/warehouse-service/order/" + orderID + "/reservation/warehouse/2";
                request = (HttpWebRequest)WebRequest.Create(uri);
                request.Method = "POST";
                request.ContentType = "application/json";
                request.Headers.Add("brightpearl-app-ref", appRef);
                request.Headers.Add("brightpearl-account-token", appToken);

                // generate JSON file for order row post
                string textJSON;
                if (productId != null)
                {
                    textJSON = "{\"products\": [{\"productId\": \"" + productId + "\",\"salesOrderRowId\": \"" + orderRowID + "\",\"quantity\":\"" + value.Quantity + "\"}]}";
                }
                else
                {
                    return;
                }

                // turn request string into a byte stream
                byte[] postBytes = Encoding.UTF8.GetBytes(textJSON);

                // send request
                using (Stream requestStream = request.GetRequestStream())
                {
                    requestStream.Write(postBytes, 0, postBytes.Length);
                }

                // get response from the server to see if there has error or not
                try
                {
                    response = (HttpWebResponse)request.GetResponse();
                }
                catch
                {
                    HasError = true;
                    return;
                }

                // reset has error to false
                HasError = false;
            }

            /* post receipt to API */
            public void postReceipt(string orderID, string contactID, BPvalues value)
            {
                string uri = "https://ws-use.brightpearl.com/2.0.0/ashlintest/accounting-service/sales-receipt";
                request = (HttpWebRequest)WebRequest.Create(uri);
                request.Method = "POST";
                request.ContentType = "application/json";
                request.Headers.Add("brightpearl-app-ref", appRef);
                request.Headers.Add("brightpearl-account-token", appToken);

                string textJSON = "{\"orderId\":\"" + orderID + "\",\"customerId\":\"" + contactID + "\",\"received\":{\"currency\":\"CAD\",\"value\":\"" + value.TotalPaid + "\"},\"bankAccountNominalCode\":\"1001\",\"channelId\":1,\"taxDate\":\"" + value.PlaceOn.ToString("yyyy-MM-dd") + "T00:00:00+00:00\"}";

                // turn request string into a byte stream
                byte[] postBytes = Encoding.UTF8.GetBytes(textJSON);

                // send request
                using (Stream requestStream = request.GetRequestStream())
                {
                    requestStream.Write(postBytes, 0, postBytes.Length);
                }

                // get response from server to see if there is error or not
                try
                {
                    response = (HttpWebResponse)request.GetResponse();
                }
                catch
                {
                    HasError = true;
                    return;
                }

                // reset has error to false
                HasError = false;
            }
        }
    }
}