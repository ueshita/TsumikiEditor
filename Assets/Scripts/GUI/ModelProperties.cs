using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Linq;

public class ModelProperties : MonoBehaviour
{
	public InputField offsetX;
	public InputField offsetY;
	public InputField offsetZ;
	public InputField rotation;
	public InputField scale;
	
	private float? originalOffsetX;
	private float? originalOffsetY;
	private float? originalOffsetZ;
	private int? originalRotation;
	private float? originalScale;

	private Model[] models;
	private Vector3[] originalOffsets;
	private int[] originalRotations;
	private float[] originalScales;

	public bool IsFocused() {
		return this.offsetX.isFocused ||
			   this.offsetY.isFocused ||
			   this.offsetZ.isFocused ||
			   this.rotation.isFocused ||
			   this.scale.isFocused;
	}

	public void SetModels(Model[] models) {
		this.models = models;
		this.UpdateViews();
	}

	public void UpdateViews() {
		if (this.models == null || this.models.Length == 0) {
			this.gameObject.SetActive(false);
			return;
		}
		this.gameObject.SetActive(true);

		Model baseModel = this.models[0];
		this.originalOffsetX = baseModel.offset.x;
		this.originalOffsetY = baseModel.offset.y;
		this.originalOffsetZ = baseModel.offset.z;
		this.originalRotation = baseModel.rotation;
		this.originalScale = baseModel.scale;

		foreach (Model model in this.models) {
			if (baseModel.offset.x != model.offset.x) this.originalOffsetX = null;
			if (baseModel.offset.y != model.offset.y) this.originalOffsetY = null;
			if (baseModel.offset.z != model.offset.z) this.originalOffsetZ = null;
			if (baseModel.rotation != model.rotation) this.originalRotation = null;
			if (baseModel.scale != model.scale) this.originalScale = null;
		}
		
		this.offsetX.text = (this.originalOffsetX.HasValue) ? baseModel.offset.x.ToString() : "-";
		this.offsetY.text = (this.originalOffsetY.HasValue) ? baseModel.offset.y.ToString() : "-";
		this.offsetZ.text = (this.originalOffsetZ.HasValue) ? baseModel.offset.z.ToString() : "-";
		this.rotation.text = (this.originalRotation.HasValue) ? baseModel.rotation.ToString() : "-";
		this.scale.text = (this.originalScale.HasValue) ? this.models[0].scale.ToString() : "-";

		this.originalOffsets = this.models.Select(o => o.offset).ToArray();
		this.originalRotations = this.models.Select(o => o.rotation).ToArray();
		this.originalScales = this.models.Select(o => o.scale).ToArray();
	}

	public void PositionX_OnValueChanged() {
		float value;
		if (float.TryParse(this.offsetX.text, out value)) {
			this.models.ForEach((o, i) => o.SetOffset(new Vector3(
				value, 
				this.originalOffsets[i].y, 
				this.originalOffsets[i].z
			)));
			EditManager.Instance.Selector.SetDirty();
		}
	}
	public void PositionY_OnValueChanged() {
		float value;
		if (float.TryParse(this.offsetY.text, out value)) {
			this.models.ForEach((o, i) => o.SetOffset(new Vector3(
				this.originalOffsets[i].x, 
				value, 
				this.originalOffsets[i].z
			)));
			EditManager.Instance.Selector.SetDirty();
		}
	}
	public void PositionZ_OnValueChanged() {
		float value;
		if (float.TryParse(this.offsetZ.text, out value)) {
			this.models.ForEach((o, i) => o.SetOffset(new Vector3(
				this.originalOffsets[i].x, 
				this.originalOffsets[i].y, 
				value
			)));
			EditManager.Instance.Selector.SetDirty();
		}
	}
	public void Rotation_OnValueChanged() {
		float value;
		if (float.TryParse(this.rotation.text, out value)) {
			this.models.ForEach((o, i) => o.SetRotation((int)value));
			EditManager.Instance.Selector.SetDirty();
		}
	}
	public void Scale_OnValueChanged() {
		float value;
		if (float.TryParse(this.scale.text, out value)) {
			this.models.ForEach((o, i) => o.SetScale(value));
			EditManager.Instance.Selector.SetDirty();
		}
	}

	public void PositionX_OnEditEnd() {
		this.models.ForEach((o, i) => o.SetOffset(this.originalOffsets[i]));
		float value;
		if (float.TryParse(this.offsetX.text, out value)) {
			EditManager.Instance.SetModelOffsetX(this.models, value);
			this.UpdateViews();
		}
	}
	public void PositionY_OnEditEnd() {
		this.models.ForEach((o, i) => o.SetOffset(this.originalOffsets[i]));
		float value;
		if (float.TryParse(this.offsetY.text, out value)) {
			EditManager.Instance.SetModelOffsetY(this.models, value);
			this.UpdateViews();
		}
	}
	public void PositionZ_OnEditEnd() {
		this.models.ForEach((o, i) => o.SetOffset(this.originalOffsets[i]));
		float value;
		if (float.TryParse(this.offsetZ.text, out value)) {
			EditManager.Instance.SetModelOffsetZ(this.models, value);
			this.UpdateViews();
		}
	}
	public void Rotation_OnEditEnd() {
		this.models.ForEach((o, i) => o.SetRotation(this.originalRotations[i]));
		float value;
		if (float.TryParse(this.rotation.text, out value)) {
			EditManager.Instance.SetModelRotation(this.models, (int)value);
			this.UpdateViews();
		}
	}
	public void Scale_OnEditEnd() {
		this.models.ForEach((o, i) => o.SetScale(this.originalScales[i]));
		float value;
		if (float.TryParse(this.scale.text, out value)) {
			EditManager.Instance.SetModelScale(this.models, value);
			this.UpdateViews();
		}
	}
}
