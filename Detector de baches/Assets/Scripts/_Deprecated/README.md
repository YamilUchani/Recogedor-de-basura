# Scripts Deprecados

Esta carpeta contiene scripts que **NO están siendo utilizados** en ninguna escena o prefab del proyecto.

## ⚠️ Importante

Los scripts en esta carpeta se mantienen en el proyecto por las siguientes razones:

1. **Referencia histórica** - Pueden contener lógica útil para futuras implementaciones
2. **Backup de código** - Preservar trabajo previo antes de eliminar permanentemente
3. **Documentación** - Entender decisiones de diseño anteriores

## 📁 Organización

### Mesh/ (7 scripts)
Utilidades de generación y manipulación de meshes que fueron reemplazadas o no se usan.

### Gameplay/ (6 scripts)
Mecánicas de juego de prototipos anteriores o funcionalidades descartadas.

### Lighting/ (3 scripts)
Sistema de iluminación que fue reemplazado o no se implementó.

### UI/ (2 scripts)
Componentes de UI que no se están usando actualmente.

### Terrain/ (1 script)
Funcionalidad de terreno que no se utiliza.

### Other/ (3 scripts)
Scripts misceláneos sin categoría específica.

## 🔄 Proceso de Revisión

Antes de eliminar permanentemente estos scripts:

1. ✅ Verificar que no hay referencias dinámicas en código (AddComponent, etc.)
2. ✅ Confirmar que la funcionalidad no se necesitará en el futuro
3. ✅ Documentar cualquier lógica importante antes de eliminar
4. ✅ Crear un commit de backup antes de la eliminación

## 📝 Notas

- Fecha de deprecación: 2026-02-01
- Estos scripts fueron identificados mediante análisis automático de escenas y prefabs
- Si necesitas usar alguno de estos scripts, muévelo a la carpeta apropiada en Scripts/

---

**¿Dudas sobre algún script?** Revisa el archivo `analisis_scripts.md` en la carpeta brain para más detalles.
