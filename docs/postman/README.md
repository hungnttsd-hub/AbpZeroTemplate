# Shopee AMS permission check

Files:

- `Shopee-AMS-Permission-Check.postman_collection.json`
- `Shopee-AMS-Local.postman_environment.json`

## Run the check

1. Import both JSON files into Postman.
2. Select the **Shopee AMS Local** environment.
3. Set the current values for `partner_id`, `shop_id`, and `access_token`.
4. Set either `partner_key`, or both `manual_timestamp` and `manual_sign` if a
   third party signs the exact request for you.
5. Open **Check AMS conversion-report permission** and select **Send**.

When `partner_key` is present, the collection generates a new timestamp and
HMAC-SHA256 signature for every request using this Shopee shop-level base string:

```text
partner_id + api_path + timestamp + access_token + shop_id
```

If you only have a supplied signature, it must have been calculated for this
exact API path, partner ID, access token, shop ID, and `manual_timestamp`. Shopee
expires the timestamp after five minutes, so a static signature cannot be reused.

The request queries orders placed during the most recent `days_back` days. The
default is seven days and the collection limits this value to 89 days.

## Interpret the result

- `error` is empty: the app and shop token can call the AMS endpoint. An empty
  `response.list` still means the permission check succeeded.
- Permission or access denied: the partner app is not enabled for **Affiliate
  Marketing Solution Management**, or the app is not permitted for this shop.
- Invalid access token or shop authorization: refresh/re-authorize the shop and
  retry before deciding whether AMS permission exists.
- Invalid sign or timestamp: verify the partner key, partner ID, shop ID, and
  system clock. This does not prove that AMS permission is missing.

Postman stores the latest `error`, `message`, and `request_id` in the environment
as `last_error`, `last_message`, and `last_request_id`.

## Security

Treat `partner_key` and `access_token` as passwords. Do not send them in chat,
commit a populated environment file, or expose them in frontend code. Clear the
environment current values after testing on a shared computer.

If a third party supplied only a ready-made `sign`, they must also supply its
matching timestamp and regenerate both for each exact request. For a durable
integration, ask them to expose a server-side API that performs the signed
Shopee request instead of sharing the partner key.
