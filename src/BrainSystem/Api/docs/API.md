# API guide

All API routes produce JSON. Development Swagger UI is at `/swagger`.

## Security

Set `Security__ApiKey` in the environment for deployments. When it is set, add the matching `X-Api-Key` header to `POST /api/train`. Keep the service behind TLS and a network boundary. The other endpoints are rate-limited but should still be protected by your gateway if exposed publicly.

## Example requests

```bash
curl http://localhost:5080/api/status
curl http://localhost:5080/api/predict/10
curl -H "X-Api-Key: $BRAIN_SYSTEM_API_KEY" \
  -H "Content-Type: application/json" \
  -d '{"epochs":4,"learningRate":0.01,"samples":256}' \
  http://localhost:5080/api/train
curl -H "Content-Type: application/json" \
  -d '{"message":"/tool calculate {\"expression\":\"12 * (4 + 1)\"}"}' \
  http://localhost:5080/api/chat
```

The predictable `/tool <name> <json>` chat convention intentionally avoids handing untrusted text to an unrestricted tool parser. Tool names: `calculate`, `utc_time`, `unit_convert`, `predict_drift`.

## Output constraints

Predictions are bounded research signals. The API never submits orders, controls accounts, or provides personalized financial recommendations.