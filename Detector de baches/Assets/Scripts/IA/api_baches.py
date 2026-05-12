import cv2
import numpy as np
import torch
import torch.nn as nn
import uvicorn
from contextlib import asynccontextmanager
from fastapi import FastAPI, UploadFile, File
from fastapi.responses import JSONResponse
from ultralytics import YOLO
from torchvision import transforms
import os



@asynccontextmanager
async def lifespan(app: FastAPI):
    # Load models
    load_models_on_startup()
    yield

app = FastAPI(lifespan=lifespan)

DEVICE      = "cuda" if torch.cuda.is_available() else "cpu"
IMG_SIZE    = 640
CROP_SIZE   = 256
DET_CLASSES  = ["Pothole-general", "Person", "Car"]
CRACK_CLASSES = ["Crocodile Crack", "Single Crack", "Pothole"]

# Carpeta para guardar resultados
BASE_DIR = os.path.dirname(os.path.abspath(__file__))
DETECTIONS_DIR = os.path.join(BASE_DIR, "Detecciones_IA")
if not os.path.exists(DETECTIONS_DIR):
    os.makedirs(DETECTIONS_DIR)

val_transform = transforms.Compose([
    transforms.ToPILImage(),
    transforms.Resize((CROP_SIZE, CROP_SIZE)),
    transforms.ToTensor(),
    transforms.Normalize([0.485, 0.456, 0.406], [0.229, 0.224, 0.225])
])

class ExportableClassifier(nn.Module):
    def __init__(self, backbone, pool, cls_head):
        super().__init__()
        self.backbone = backbone
        self.pool     = pool
        self.cls_head = cls_head

    def forward(self, x):
        feat = x
        for layer in self.backbone:
            feat = layer(feat)
        feat = self.pool(feat)
        return self.cls_head(feat)

# Variables globales para los modelos
detector = None
classifier = None

def load_models_on_startup():
    global detector, classifier
    
    # Obtener la ruta absoluta de la carpeta donde está este script
    BASE_DIR = os.path.dirname(os.path.abspath(__file__))
    
    det_pt_path = os.path.join(BASE_DIR, "detector_temp.pt")
    cls_pt_path = os.path.join(BASE_DIR, "best_multitask_model.pt")
    
    print(f"Cargando modelos: {det_pt_path} y {cls_pt_path} en {DEVICE}...")
    
    # 1. Cargar YOLO (Detector)
    detector  = YOLO(det_pt_path)
    yolo_nn   = YOLO(det_pt_path).model.to(DEVICE)
    for p in yolo_nn.parameters():
        p.requires_grad = False
    yolo_nn.eval()

    # 2. Armar el Clasificador a mano (como lo hizo el equipo)
    backbone = yolo_nn.model[:10]
    pool     = nn.AdaptiveAvgPool2d((1, 1))
    cls_head = nn.Sequential(
        nn.Flatten(),
        nn.Linear(256, 256), nn.ReLU(), nn.Dropout(0.4),
        nn.Linear(256, 128), nn.ReLU(), nn.Dropout(0.3),
        nn.Linear(128, 3)
    )
    
    ckpt = torch.load(cls_pt_path, map_location=DEVICE, weights_only=True)
    cls_head.load_state_dict(ckpt["cls_head"])
    pool.load_state_dict(ckpt["pool"])
    cls_head.eval()
    pool.eval()

    classifier = ExportableClassifier(backbone, pool, cls_head).to(DEVICE)
    classifier.eval()
    print("¡Modelos cargados correctamente! Servidor listo.")

@app.post("/predict")
async def predict_image(file: UploadFile = File(...)):
    contents = await file.read()
    nparr = np.frombuffer(contents, np.uint8)
    img = cv2.imdecode(nparr, cv2.IMREAD_COLOR)
    
    # Inferencia
    results = detector.predict(img, imgsz=IMG_SIZE, conf=0.25, verbose=False)
    detections = []

    for result in results:
        for box in result.boxes:
            cls_id     = int(box.cls.item())
            conf_score = float(box.conf.item())
            x1, y1, x2, y2 = map(int, box.xyxy[0].tolist())

            if cls_id == 0: # Pothole-general
                crop = img[max(0,y1):min(img.shape[0],y2), max(0,x1):min(img.shape[1],x2)]
                
                if crop.size > 0:
                    tensor = val_transform(
                        cv2.cvtColor(crop, cv2.COLOR_BGR2RGB)
                    ).unsqueeze(0).to(DEVICE)
                    
                    with torch.no_grad():
                        crack_cls = classifier(tensor).argmax(1).item()
                    final_label = CRACK_CLASSES[crack_cls]
                else:
                    final_label = "Pothole-general"
            else:
                final_label = DET_CLASSES[cls_id]

            # Mantengo el formato de llaves (clase, confianza, caja) para que Unity lo lea igual
            detections.append({
                 "clase": final_label, 
                 "confianza": round(conf_score, 3),
                 "caja":  [x1, y1, x2, y2]
            })

            # --- DIBUJAR EN LA IMAGEN ---
            color = (0, 255, 0) if "Pothole" in final_label else (255, 0, 0)
            if "Crocodile" in final_label: color = (0, 255, 255) # Amarillo para cocodrilo
            
            cv2.rectangle(img, (x1, y1), (x2, y2), color, 2)
            label_str = f"{final_label} {conf_score:.2f}"
            cv2.putText(img, label_str, (x1, y1 - 10), cv2.FONT_HERSHEY_SIMPLEX, 0.5, color, 2)

    # Guardar SIEMPRE la imagen (con o sin detecciones)
    output_path = os.path.join(DETECTIONS_DIR, file.filename)
    cv2.imwrite(output_path, img)
            
    return JSONResponse(content=detections)

if __name__ == "__main__":
    uvicorn.run(app, host="0.0.0.0", port=5000)
