# Jev API — TypeSafe AI

## URL base
https://api.typesafe.ai/v1/systemone

## Autenticación
Authorization: Bearer <API_KEY>
La API key se obtiene en https://console.typesafe.ai/keys

## Request

POST /v1/systemone
Content-Type: application/json
Authorization: Bearer <API_KEY>

```json
{
  "state": "<texto del mensaje>",
  "model": "jev-latest",
  "questions": {
    "<nombre>": {
      "type": "choice | score | noul",
      "instructions": "<descripción de qué evaluar>",
      "criteria": { ... }
    }
  }
}
```

### Tipos de preguntas
- **noul**: booleano (0 o 1)
- **choice**: selección entre opciones predefinidas en `criteria` (objeto clave→descripción)
- **score**: puntuación numérica, `criteria` es array de descripciones por nivel

## Response

```json
{
  "model": "jev-1.13.0",
  "answers": {
    "<nombre>": {
      "type": "choice",
      "choice": "<clave elegida>",
      "confidence": 0.78,
      "probabilities": { "<clave>": 0.0, ... }
    }
  },
  "usage": {
    "input_tokens": 392,
    "output_tokens": 65
  }
}
```

## Cómo usamos Jev en MessageGuard

Enviamos una pregunta tipo `choice` con las cuatro clasificaciones del dominio:

```json
{
  "state": "<content del mensaje>",
  "model": "jev-latest",
  "questions": {
    "classification": {
      "type": "choice",
      "instructions": "Classify this help request message",
      "criteria": {
        "spam": "Promotional, unsolicited or irrelevant content",
        "threat": "Contains threatening language, violence or intent to cause harm",
        "urgent": "Emergency situation that requires immediate attention",
        "normal": "Regular help request that should be processed normally"
      }
    }
  }
}
```

La respuesta se lee en `answers.classification.choice` → se mapea a `Classification` enum.

## Regla de seguridad
Si Jev falla o no responde, el mensaje queda en estado REJECTED_REVIEW (revisión manual).
Nunca se envía sin clasificación exitosa.
