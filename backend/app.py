from __future__ import annotations

import os
from datetime import datetime, timedelta, timezone
from enum import Enum
from typing import Dict, List, Optional
from uuid import uuid4

from fastapi import (
    Depends,
    FastAPI,
    File,
    HTTPException,
    UploadFile,
    WebSocket,
    WebSocketDisconnect,
    status,
)
from fastapi.security import OAuth2PasswordBearer, OAuth2PasswordRequestForm
from jose import JWTError, jwt
from passlib.context import CryptContext
from pydantic import BaseModel, Field

SECRET_KEY = os.getenv("JWT_SECRET", "dev-secret")
ALGORITHM = "HS256"
ACCESS_TOKEN_EXPIRE_MINUTES = 15
REFRESH_TOKEN_EXPIRE_DAYS = 7

pwd_context = CryptContext(schemes=["bcrypt"], deprecated="auto")
oauth2_scheme = OAuth2PasswordBearer(tokenUrl="/auth/login")

app = FastAPI(title="Service Apo Backend", version="0.1.0")


class Role(str, Enum):
    apotheke = "Apotheke"
    arztpraxis = "Arztpraxis"
    pflegeheim = "Pflegeheim"
    admin = "Admin"


class TokenPair(BaseModel):
    access_token: str
    refresh_token: str
    token_type: str = "bearer"


class UserBase(BaseModel):
    email: str
    full_name: Optional[str] = None
    role: Role


class UserCreate(UserBase):
    password: str = Field(min_length=8)


class UserPublic(UserBase):
    id: str


class UserInDB(UserBase):
    id: str
    hashed_password: str


class PatientResident(BaseModel):
    id: str
    full_name: str
    date_of_birth: Optional[str] = None
    care_location: Optional[str] = None


class Prescription(BaseModel):
    id: str
    patient_id: str
    medication: str
    dosage: str
    prescribed_by: str
    status: str = "active"


class PrescriptionRequest(BaseModel):
    id: str
    patient_id: str
    requester_id: str
    requested_medication: str
    notes: Optional[str] = None
    status: str = "pending"


class MedicationPlan(BaseModel):
    id: str
    patient_id: str
    medications: List[str]
    updated_by: str
    updated_at: datetime


class Message(BaseModel):
    id: str
    chat_id: str
    sender_id: str
    content: str
    sent_at: datetime


class TelemedicineSession(BaseModel):
    id: str
    patient_id: str
    scheduled_for: datetime
    host_id: str
    status: str = "scheduled"
    meeting_link: Optional[str] = None


class Attachment(BaseModel):
    id: str
    filename: str
    content_type: str
    uploaded_by: str
    uploaded_at: datetime


class MedicationPlanChangeRequest(BaseModel):
    patient_id: str
    medications: List[str]


class RefreshRequest(BaseModel):
    refresh_token: str


users: Dict[str, UserInDB] = {}
refresh_tokens: Dict[str, str] = {}
patients: Dict[str, PatientResident] = {}
prescriptions: Dict[str, Prescription] = {}
prescription_requests: Dict[str, PrescriptionRequest] = {}
medication_plans: Dict[str, MedicationPlan] = {}
messages: Dict[str, List[Message]] = {}
telemedicine_sessions: Dict[str, TelemedicineSession] = {}
attachments: Dict[str, Attachment] = {}


class ConnectionManager:
    def __init__(self) -> None:
        self.active_connections: Dict[str, List[WebSocket]] = {}

    async def connect(self, channel: str, websocket: WebSocket) -> None:
        await websocket.accept()
        self.active_connections.setdefault(channel, []).append(websocket)

    def disconnect(self, channel: str, websocket: WebSocket) -> None:
        self.active_connections.get(channel, []).remove(websocket)

    async def broadcast(self, channel: str, message: dict) -> None:
        for connection in self.active_connections.get(channel, []):
            await connection.send_json(message)


chat_manager = ConnectionManager()
notification_manager = ConnectionManager()


def hash_password(password: str) -> str:
    return pwd_context.hash(password)


def verify_password(plain_password: str, hashed_password: str) -> bool:
    return pwd_context.verify(plain_password, hashed_password)


def create_token(data: dict, expires_delta: timedelta) -> str:
    to_encode = data.copy()
    expire = datetime.now(timezone.utc) + expires_delta
    to_encode.update({"exp": expire})
    return jwt.encode(to_encode, SECRET_KEY, algorithm=ALGORITHM)


def get_user_by_email(email: str) -> Optional[UserInDB]:
    return next((user for user in users.values() if user.email == email), None)


def get_current_user(token: str = Depends(oauth2_scheme)) -> UserInDB:
    credentials_exception = HTTPException(
        status_code=status.HTTP_401_UNAUTHORIZED,
        detail="Could not validate credentials",
        headers={"WWW-Authenticate": "Bearer"},
    )
    try:
        payload = jwt.decode(token, SECRET_KEY, algorithms=[ALGORITHM])
        user_id: str = payload.get("sub")
        if user_id is None:
            raise credentials_exception
    except JWTError as exc:
        raise credentials_exception from exc
    user = users.get(user_id)
    if user is None:
        raise credentials_exception
    return user


def require_roles(*roles: Role):
    def dependency(user: UserInDB = Depends(get_current_user)) -> UserInDB:
        if user.role not in roles:
            raise HTTPException(status_code=403, detail="Insufficient role")
        return user

    return dependency


@app.post("/auth/register", response_model=UserPublic)
def register_user(payload: UserCreate) -> UserPublic:
    if get_user_by_email(payload.email):
        raise HTTPException(status_code=400, detail="Email already registered")
    user_id = str(uuid4())
    user = UserInDB(
        id=user_id,
        email=payload.email,
        full_name=payload.full_name,
        role=payload.role,
        hashed_password=hash_password(payload.password),
    )
    users[user_id] = user
    return UserPublic(**user.model_dump(exclude={"hashed_password"}))


@app.post("/auth/login", response_model=TokenPair)
def login(form_data: OAuth2PasswordRequestForm = Depends()) -> TokenPair:
    user = get_user_by_email(form_data.username)
    if not user or not verify_password(form_data.password, user.hashed_password):
        raise HTTPException(status_code=400, detail="Incorrect username or password")
    access_token = create_token(
        {"sub": user.id, "role": user.role},
        timedelta(minutes=ACCESS_TOKEN_EXPIRE_MINUTES),
    )
    refresh_token = create_token(
        {"sub": user.id, "scope": "refresh"},
        timedelta(days=REFRESH_TOKEN_EXPIRE_DAYS),
    )
    refresh_tokens[refresh_token] = user.id
    return TokenPair(access_token=access_token, refresh_token=refresh_token)


@app.post("/auth/refresh", response_model=TokenPair)
def refresh_token(payload: RefreshRequest) -> TokenPair:
    stored_user_id = refresh_tokens.get(payload.refresh_token)
    if not stored_user_id:
        raise HTTPException(status_code=401, detail="Invalid refresh token")
    try:
        decoded = jwt.decode(payload.refresh_token, SECRET_KEY, algorithms=[ALGORITHM])
        if decoded.get("scope") != "refresh":
            raise HTTPException(status_code=401, detail="Invalid refresh token")
    except JWTError as exc:
        raise HTTPException(status_code=401, detail="Invalid refresh token") from exc
    user = users.get(stored_user_id)
    if not user:
        raise HTTPException(status_code=404, detail="User not found")
    access_token = create_token(
        {"sub": user.id, "role": user.role},
        timedelta(minutes=ACCESS_TOKEN_EXPIRE_MINUTES),
    )
    refresh_token_value = create_token(
        {"sub": user.id, "scope": "refresh"},
        timedelta(days=REFRESH_TOKEN_EXPIRE_DAYS),
    )
    refresh_tokens[refresh_token_value] = user.id
    return TokenPair(access_token=access_token, refresh_token=refresh_token_value)


@app.post("/patients", response_model=PatientResident)
def create_patient(
    payload: PatientResident,
    user: UserInDB = Depends(require_roles(Role.pflegeheim, Role.arztpraxis, Role.admin)),
) -> PatientResident:
    patients[payload.id] = payload
    return payload


@app.get("/patients", response_model=List[PatientResident])
def list_patients(
    user: UserInDB = Depends(require_roles(Role.apotheke, Role.arztpraxis, Role.pflegeheim, Role.admin)),
) -> List[PatientResident]:
    return list(patients.values())


@app.get("/patients/{patient_id}", response_model=PatientResident)
def get_patient(
    patient_id: str,
    user: UserInDB = Depends(require_roles(Role.apotheke, Role.arztpraxis, Role.pflegeheim, Role.admin)),
) -> PatientResident:
    patient = patients.get(patient_id)
    if not patient:
        raise HTTPException(status_code=404, detail="Patient not found")
    return patient


@app.post("/prescription-requests", response_model=PrescriptionRequest)
def create_prescription_request(
    payload: PrescriptionRequest,
    user: UserInDB = Depends(require_roles(Role.pflegeheim, Role.arztpraxis, Role.admin)),
) -> PrescriptionRequest:
    prescription_requests[payload.id] = payload
    return payload


@app.get("/prescription-requests", response_model=List[PrescriptionRequest])
def list_prescription_requests(
    user: UserInDB = Depends(require_roles(Role.apotheke, Role.arztpraxis, Role.admin)),
) -> List[PrescriptionRequest]:
    return list(prescription_requests.values())


@app.post("/prescriptions", response_model=Prescription)
def create_prescription(
    payload: Prescription,
    user: UserInDB = Depends(require_roles(Role.arztpraxis, Role.admin)),
) -> Prescription:
    prescriptions[payload.id] = payload
    return payload


@app.get("/prescriptions", response_model=List[Prescription])
def list_prescriptions(
    user: UserInDB = Depends(require_roles(Role.apotheke, Role.arztpraxis, Role.pflegeheim, Role.admin)),
) -> List[Prescription]:
    return list(prescriptions.values())


@app.post("/medication-plans/{patient_id}/changes", response_model=MedicationPlan)
def update_medication_plan(
    patient_id: str,
    payload: MedicationPlanChangeRequest,
    user: UserInDB = Depends(require_roles(Role.apotheke, Role.arztpraxis, Role.admin)),
) -> MedicationPlan:
    plan = MedicationPlan(
        id=str(uuid4()),
        patient_id=patient_id,
        medications=payload.medications,
        updated_by=user.id,
        updated_at=datetime.now(timezone.utc),
    )
    medication_plans[patient_id] = plan
    return plan


@app.post("/orders", response_model=Prescription)
def create_order(
    payload: Prescription,
    user: UserInDB = Depends(require_roles(Role.apotheke, Role.admin)),
) -> Prescription:
    prescriptions[payload.id] = payload
    return payload


@app.post("/chats/{chat_id}/messages", response_model=Message)
def send_message(
    chat_id: str,
    payload: Message,
    user: UserInDB = Depends(require_roles(Role.apotheke, Role.arztpraxis, Role.pflegeheim, Role.admin)),
) -> Message:
    messages.setdefault(chat_id, []).append(payload)
    return payload


@app.get("/chats/{chat_id}/messages", response_model=List[Message])
def list_messages(
    chat_id: str,
    user: UserInDB = Depends(require_roles(Role.apotheke, Role.arztpraxis, Role.pflegeheim, Role.admin)),
) -> List[Message]:
    return messages.get(chat_id, [])


@app.post("/attachments/upload", response_model=Attachment)
def upload_attachment(
    file: UploadFile = File(...),
    user: UserInDB = Depends(require_roles(Role.apotheke, Role.arztpraxis, Role.pflegeheim, Role.admin)),
) -> Attachment:
    attachment = Attachment(
        id=str(uuid4()),
        filename=file.filename,
        content_type=file.content_type or "application/octet-stream",
        uploaded_by=user.id,
        uploaded_at=datetime.now(timezone.utc),
    )
    attachments[attachment.id] = attachment
    return attachment


@app.post("/telemedicine-sessions", response_model=TelemedicineSession)
def create_telemedicine_session(
    payload: TelemedicineSession,
    user: UserInDB = Depends(require_roles(Role.arztpraxis, Role.admin)),
) -> TelemedicineSession:
    telemedicine_sessions[payload.id] = payload
    return payload


@app.get("/telemedicine-sessions", response_model=List[TelemedicineSession])
def list_telemedicine_sessions(
    user: UserInDB = Depends(require_roles(Role.apotheke, Role.arztpraxis, Role.pflegeheim, Role.admin)),
) -> List[TelemedicineSession]:
    return list(telemedicine_sessions.values())


@app.websocket("/ws/chat/{chat_id}")
async def chat_socket(websocket: WebSocket, chat_id: str) -> None:
    await chat_manager.connect(chat_id, websocket)
    try:
        while True:
            data = await websocket.receive_json()
            await chat_manager.broadcast(chat_id, data)
    except WebSocketDisconnect:
        chat_manager.disconnect(chat_id, websocket)


@app.websocket("/ws/notifications")
async def notifications_socket(websocket: WebSocket) -> None:
    await notification_manager.connect("notifications", websocket)
    try:
        while True:
            data = await websocket.receive_json()
            await notification_manager.broadcast("notifications", data)
    except WebSocketDisconnect:
        notification_manager.disconnect("notifications", websocket)
