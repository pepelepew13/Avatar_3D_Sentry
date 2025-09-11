from pathlib import Path

# Determine the root directory of the project
ROOT_DIR = Path(__file__).resolve().parent.parent

# Ensure the output directory exists
output_dir = ROOT_DIR / 'wwwroot' / 'models'
output_dir.mkdir(parents=True, exist_ok=True)

# Path to the output GLB file containing shape keys
output_path = output_dir / 'avatar_shape_keys.glb'

# Additional logic for generating shape keys would go here.