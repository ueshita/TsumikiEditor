using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class ToolMenu : MonoBehaviour
{
	public List<Toggle> toolMenu;

	public void SetTool(int id) {
		if (id >= 0 && id < this.toolMenu.Count) {
			this.toolMenu[id].isOn = true;
		}
	}
	public void ToolPen_OnValueChanged(Toggle tgl) {
		if (tgl.isOn) EditManager.Instance.SetTool(EditManager.Tool.Block);
	}
	public void ToolEraser_OnValueChanged(Toggle tgl) {
		if (tgl.isOn) EditManager.Instance.SetTool(EditManager.Tool.Eraser);
	}
	public void ToolBrush_OnValueChanged(Toggle tgl) {
		if (tgl.isOn) EditManager.Instance.SetTool(EditManager.Tool.Brush);
	}
	public void ToolSpuit_OnValueChanged(Toggle tgl) {
		if (tgl.isOn) EditManager.Instance.SetTool(EditManager.Tool.Spuit);
	}
	public void ToolPointSelect_OnValueChanged(Toggle tgl) {
		if (tgl.isOn) EditManager.Instance.SetTool(EditManager.Tool.PointSelector);
	}
	public void ToolRoutePath_OnValueChanged(Toggle tgl) {
		if (tgl.isOn) EditManager.Instance.SetTool(EditManager.Tool.RoutePath);
	}
	public void ToolModel_OnValueChanged(Toggle tgl) {
		if (tgl.isOn) EditManager.Instance.SetTool(EditManager.Tool.Model);
	}
	public void ToolMetaInfo_OnValueChanged(Toggle tgl) {
		if (tgl.isOn) EditManager.Instance.SetTool(EditManager.Tool.MetaInfo);
	}
}
