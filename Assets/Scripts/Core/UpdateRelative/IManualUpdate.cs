using System;
using TrueSync;

namespace Core.UpdateRelative
{
	public interface IManualUpdate:IComparable<IManualUpdate>
	{
		/// <summary>
		/// 表示是否销毁，请在正常情况下别设置这个值，会有系统自动设置
		/// </summary>
		bool Destroyed { get; set; }
		/// <summary>
		/// 表示是否准备销毁，可以用来写准备销毁的条件
		/// </summary>
		bool ReadyDestroy { get; }
		ManualScriptExecutionOrderEnum ScriptExecutionOrder { get; }
		void ManualAwake();
		void ManualStart();
		void ManualFixedUpdate();
		void ManualDestroy();
		void OnPause();
		void OnResume();
		void ManualUpdate(float lerpVal=default);//手动渲染这一帧到下一帧的插值，会传入比例
		bool ManualCheckNull();
	}
}