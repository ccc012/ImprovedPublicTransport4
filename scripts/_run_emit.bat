@echo off
cd /d "%~dp0"
python emit_packs.py
python -c "from pathlib import Path; import re,sys; sys.path.insert(0,'.'); from lang_packs_all import NO, emit; emit('no', NO)"
python -c "import sys; sys.path.insert(0,'.'); from lang_packs_sv_fi import SV, FI; from lang_packs_all import emit; emit('sv', SV); emit('fi', FI)"
python -c "import sys; sys.path.insert(0,'.'); from lang_packs_hu_ro import HU; from lang_packs_all import emit; emit('hu', HU)"
python build_from_siblings.py
pause
