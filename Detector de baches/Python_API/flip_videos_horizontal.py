import cv2
import os
import glob

recordings_dir = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "DigitalTwin_Logs", "Recordings")

episode_folders = sorted(glob.glob(os.path.join(recordings_dir, "Ep_*")))

print(f"Found {len(episode_folders)} episode folders: {[os.path.basename(f) for f in episode_folders]}")

for ep_folder in episode_folders:
    video_files = glob.glob(os.path.join(ep_folder, "*.mp4"))
    if not video_files:
        print(f"No MP4 files found in {ep_folder}, skipping.")
        continue
    
    for video_path in video_files:
        video_name = os.path.basename(video_path)
        name_no_ext = os.path.splitext(video_name)[0]
        output_path = os.path.join(ep_folder, f"{name_no_ext}_flipped.mp4")
        
        print(f"Processing: {video_name} -> {os.path.basename(output_path)}")
        
        cap = cv2.VideoCapture(video_path)
        if not cap.isOpened():
            print(f"ERROR: Could not open {video_path}")
            continue
        
        fps = int(cap.get(cv2.CAP_PROP_FPS))
        width = int(cap.get(cv2.CAP_PROP_FRAME_WIDTH))
        height = int(cap.get(cv2.CAP_PROP_FRAME_HEIGHT))
        total_frames = int(cap.get(cv2.CAP_PROP_FRAME_COUNT))
        
        print(f"  Resolution: {width}x{height}, FPS: {fps}, Total frames: {total_frames}")
        
        fourcc = cv2.VideoWriter_fourcc(*'mp4v')
        out = cv2.VideoWriter(output_path, fourcc, fps, (width, height))
        
        frame_count = 0
        while True:
            ret, frame = cap.read()
            if not ret:
                break
            
            # Flip horizontally (mirror effect)
            flipped_frame = cv2.flip(frame, 1)
            out.write(flipped_frame)
            frame_count += 1
            
            if frame_count % 100 == 0:
                print(f"  Processed {frame_count}/{total_frames} frames")
        
        cap.release()
        out.release()
        print(f"  Done! {frame_count} frames processed and saved to {os.path.basename(output_path)}")

print("\nAll videos flipped horizontally successfully!")