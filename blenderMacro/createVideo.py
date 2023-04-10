from datetime import datetime
import os
import pprint

import bpy


def clean_sequencer(sequence_context):
    bpy.ops.sequencer.select_all(sequence_context, action="SELECT")
    bpy.ops.sequencer.delete(sequence_context)


def find_sequence_editor():
    for area in bpy.context.window.screen.areas:
        if area.type == "SEQUENCE_EDITOR":
            return area
    return None
def find_graph_editor():
    for area in bpy.context.window.screen.areas:
        if area.type == "GRAPH_EDITOR":
            return area
    return None

def get_image_files(image_folder_path, image_extention=".png"):
    image_files = list()
    for file_name in os.listdir(image_folder_path):
        if file_name.endswith(image_extention):
            image_files.append(file_name)
    image_files.sort(reverse=True)

    pprint.pprint(image_files)

    return image_files


def get_image_dimensions(image_path):
    image = bpy.data.images.load(image_path)
    width, height = image.size
    return width, height


def set_up_output_params(image_folder_path, image_files, fps,framesPerImage):
    image_count = len(image_files)

    scene = bpy.context.scene

    scene.frame_end = image_count*framesPerImage

    image_path = os.path.join(image_folder_path, image_files[0])

    width, height = get_image_dimensions(image_path)

    scene.render.resolution_y = int(height/2)
    scene.render.resolution_x = int(width/2)

    scene.render.fps = fps
    scene.render.image_settings.file_format = "FFMPEG"
    scene.render.ffmpeg.format = "MPEG4"
    scene.render.ffmpeg.constant_rate_factor = "PERC_LOSSLESS"

    now = datetime.now()
    time = now.strftime("%H-%M-%S")
    filepath = os.path.join(image_folder_path, f"fractalZoom_{time}.mp4")
    scene.render.filepath = filepath


def gen_video_from_images(image_folder_path, fps, framesPerImage):

    image_files = get_image_files(image_folder_path)

    set_up_output_params(image_folder_path, image_files, fps,framesPerImage)

    sequence_editor = find_sequence_editor()

    sequence_editor_context = {
        "area": sequence_editor,
    }
    clean_sequencer(sequence_editor_context)

 
    for id, image_name in enumerate(image_files):
        
        bpy.ops.sequencer.image_strip_add(
        sequence_editor_context,
        directory=image_folder_path + os.sep,
        files=[{"name": image_name}],
        frame_start=id*framesPerImage,
        frame_end=(id+1)*framesPerImage,
        )
        
    for strip in bpy.context.scene.sequence_editor.sequences_all:
        start_scale = 0.5
        end_scale = 1
        
     
      
        strip.transform.scale_x = start_scale
        strip.transform.scale_y = start_scale
        strip.transform.keyframe_insert(data_path='scale_x', frame=strip.frame_start)
        strip.transform.keyframe_insert(data_path='scale_y', frame=strip.frame_start)
        strip.transform.scale_x = end_scale
        strip.transform.scale_y = end_scale
        strip.transform.keyframe_insert(data_path='scale_x', frame=strip.frame_final_end)
        strip.transform.keyframe_insert(data_path='scale_y', frame=strip.frame_final_end)
        
   
    area = bpy.context.area
    old_type = area.type
    area.type = 'SEQUENCE_EDITOR'
    bpy.ops.sequencer.select_all(action='SELECT')
    area.type = 'GRAPH_EDITOR'
    bpy.ops.graph.select_all(action='SELECT')
    bpy.ops.graph.interpolation_type(type='LINEAR')
    area.type = old_type
    
    
    bpy.ops.render.render(animation=True)


def main():
   
    image_folder_path = "C:\\Users\\adams\\Documents\\GitHub\\Infinite_Fractal_Unity\\renders"

    framesPerImage = 20;
    fps = 30;
    gen_video_from_images(image_folder_path, fps,framesPerImage)
 

if __name__ == "__main__":
    main()