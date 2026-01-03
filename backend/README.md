# Service Apo Backend

FastAPI backend scaffold for Service Apo.

## Setup

```bash
python -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
uvicorn app:app --reload --port 8000
```

OpenAPI/Swagger docs are available at:

- Swagger UI: `http://localhost:8000/docs`
- ReDoc: `http://localhost:8000/redoc`

## Authentication

- `POST /auth/register` – Create a user with a role.
- `POST /auth/login` – OAuth2 password flow (returns access + refresh tokens).
- `POST /auth/refresh` – Exchange refresh token for a new pair.

## Core Resources

### Patients / Residents

- `POST /patients` – Create patient/resident.
- `GET /patients` – List all patients/residents.
- `GET /patients/{patient_id}` – Get patient/resident.

### Prescription Requests

- `POST /prescription-requests` – Create a request.
- `GET /prescription-requests` – List requests.

### Prescriptions / Orders

- `POST /prescriptions` – Create prescription.
- `GET /prescriptions` – List prescriptions.
- `POST /orders` – Create order from prescription.

### Medication Plan Changes

- `POST /medication-plans/{patient_id}/changes` – Update medication plan for a patient.

### Chat

- `POST /chats/{chat_id}/messages` – Send a message.
- `GET /chats/{chat_id}/messages` – List messages.

### Attachments

- `POST /attachments/upload` – Upload file metadata.

### Telemedicine Sessions

- `POST /telemedicine-sessions` – Create session.
- `GET /telemedicine-sessions` – List sessions.

## WebSockets

- `/ws/chat/{chat_id}` – Real-time chat channel.
- `/ws/notifications` – Notification channel.

## Roles (RBAC)

- `Apotheke`
- `Arztpraxis`
- `Pflegeheim`
- `Admin`

Role enforcement is applied per endpoint via dependency checks.
