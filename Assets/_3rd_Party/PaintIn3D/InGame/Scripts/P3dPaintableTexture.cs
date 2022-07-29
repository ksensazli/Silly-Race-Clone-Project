using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PaintIn3D
{
	/// <summary>This component allows you to make one texture on the attached Renderer paintable.
	/// NOTE: If the texture or texture slot you want to paint is part of a shared material (e.g. prefab material), then I recommend you add the P3dMaterialCloner component to make it unique.</summary>
	[RequireComponent(typeof(Renderer))]
	[RequireComponent(typeof(P3dPaintable))]
	[HelpURL(P3dHelper.HelpUrlPrefix + "P3dPaintableTexture")]
	[AddComponentMenu(P3dHelper.ComponentMenuPrefix + "Paintable Texture")]
	public class P3dPaintableTexture : P3dLinkedBehaviour<P3dPaintableTexture>
	{
		public enum StateType
		{
			None,
			FullTextureCopy,
			LocalCommandCopy
		}

		public enum MipType
		{
			Auto,
			ForceOn,
			ForceOff
		}

		[System.Serializable] public class PaintableTextureEvent : UnityEvent<P3dPaintableTexture> {}

		/// <summary>The material index and shader texture slot name that this component will paint.</summary>
		public P3dSlot Slot { set { slot = value; } get { return slot; } } [SerializeField] private P3dSlot slot = new P3dSlot(0, "_MainTex");

		/// <summary>The UV channel this texture is mapped to.</summary>
		public P3dCoord Coord { set { coord = value; } get { return coord; } } [UnityEngine.Serialization.FormerlySerializedAs("channel")] [SerializeField] private P3dCoord coord;

		/// <summary>The group you want to associate this texture with. Only painting components with a matching group can paint this texture. This allows you to paint multiple textures at the same time with different settings (e.g. Albedo + Normal).</summary>
		public P3dGroup Group { set { group = value; } get { return group; } } [SerializeField] private P3dGroup group;

		/// <summary>This allows you to set how this texture's state is stored. This allows you to perform undo and redo operations.
		/// FullTextureCopy = A full copy of your texture will be copied for each state. This allows you to quickly undo and redo, and works with animated skinned meshes, but it uses up a lot of texture memory.
		/// LocalCommandCopy = Each paint command will be stored in local space for each state. This allows you to perform unlimited undo and redo states with minimal memory usage, because the object will be repainted from scratch. However, performance will depend on how many states must be redrawn.</summary>
		public StateType State { set { state = value; } get { return state; } } [SerializeField] private StateType state;

		/// <summary>The amount of times this texture can have its paint operations undone.</summary>
		public int StateLimit { set { stateLimit = value; } get { return stateLimit; } } [SerializeField] private int stateLimit;

		/// <summary>If you want this texture to automatically save/load, then you can set the unique save name for it here. Keep in mind this setting won't work properly with prefab spawning since all clones will share the same SaveName.</summary>
		public string SaveName { set { saveName = value; } get { return saveName; } } [SerializeField] private string saveName;

		/// <summary>Some shaders require specific shader keywords to be enabled when adding new textures. If there is no texture in your selected slot then you may need to set this keyword.</summary>
		public string ShaderKeyword { set { shaderKeyword = value; } get { return shaderKeyword; } } [SerializeField] private string shaderKeyword;

		/// <summary>The format of the created texture.</summary>
		public RenderTextureFormat Format { set { format = value; } get { return format; } } [SerializeField] private RenderTextureFormat format;

		/// <summary>The mip maps of the created texture.
		/// Auto = On or Off based on the <b>Texture</b> mip map count.
		/// ForceOn = Always enabled.
		/// ForceOff = Always disabled.</summary>
		public MipType MipMaps { set { mipMaps = value; } get { return mipMaps; } } [SerializeField] private MipType mipMaps;

		/// <summary>If you disable this, then the unpaintable areas of this texture be discarded, and thus improve painting performance.
		/// NOTE: This is on by default, because some effects may require them to be preserved.</summary>
		public bool KeepUnpaintable { set { keepUnpaintable = value; } get { return keepUnpaintable; } } [SerializeField] private bool keepUnpaintable = true;

		/// <summary>The base width of the created texture.</summary>
		public int Width { set { width = value; } get { return width; } } [SerializeField] private int width = 512;

		/// <summary>The base height of the created texture.</summary>
		public int Height { set { height = value; } get { return height; } } [SerializeField] private int height = 512;

		/// <summary>When activated or cleared, this paintable texture will be given this color.
		/// NOTE: If <b>Texture</b> is set, then each pixel RGBA value will be multiplied/tinted by this color.</summary>
		public Color Color { set { color = value; } get { return color; } } [SerializeField] private Color color = Color.white;

		/// <summary>When activated or cleared, this paintable texture will be given this texture, and then multiplied/tinted by the <b>Color</b>.
		/// None = White.</summary>
		public Texture Texture { set { texture = value; } get { return texture; } } [SerializeField] private Texture texture;

		/// <summary>This event is called after a paint command has been added to this texture. These commands will be executed at the end of the frame.</summary>
		public event System.Action<P3dCommand> OnAddCommand;

		/// <summary>This event is called after this texture has been painted, allowing you to perform actions like counting the pixels after modification.
		/// Bool = Preview painting.</summary>
		public event System.Action<bool> OnModified;

		[System.NonSerialized]
		private P3dPaintable cachedPaintable;

		[System.NonSerialized]
		private bool cachedPaintableSet;

		[SerializeField]
		private bool activated;

		[SerializeField]
		private RenderTexture current;

		[SerializeField]
		private RenderTexture preview;

		[System.NonSerialized]
		private List<P3dPaintableState> paintableStates = new List<P3dPaintableState>();

		[System.NonSerialized]
		private int stateIndex;

		[System.NonSerialized]
		private P3dPaintable paintable;

		[System.NonSerialized]
		private bool paintableSet;

		[System.NonSerialized]
		private Material material;

		[System.NonSerialized]
		private bool materialSet;

		[System.NonSerialized]
		private List<P3dCommand> paintCommands = new List<P3dCommand>();

		[System.NonSerialized]
		private List<P3dCommand> previewCommands = new List<P3dCommand>();

		[System.NonSerialized]
		private List<P3dCommand> localCommands = new List<P3dCommand>();

		[System.NonSerialized]
		private static List<P3dPaintableTexture> tempPaintableTextures = new List<P3dPaintableTexture>();

		/// <summary>This lets you know if this texture is activated and ready for painting. Activation is controlled by the associated P3dPaintable component.</summary>
		public bool Activated
		{
			get
			{
				return activated;
			}
		}

		/// <summary>This lets you know if there is at least one undo state this texture can be undone into.</summary>
		public bool CanUndo
		{
			get
			{
				return state != StateType.None && stateIndex > 0;
			}
		}

		/// <summary>This lets you know if there is at least one redo state this texture can be redone into.</summary>
		public bool CanRedo
		{
			get
			{
				return state != StateType.None && stateIndex < paintableStates.Count - 1;
			}
		}

		/// <summary>This property returns a list of all stored undo/redo states.</summary>
		public List<P3dPaintableState> States
		{
			get
			{
				return paintableStates;
			}
		}

		/// <summary>This tells you which undo/redo state is currently active inside the <b>States</b> list.</summary>
		public int StateIndex
		{
			get
			{
				return stateIndex;
			}
		}

		/// <summary>This quickly gives you the P3dPaintable component associated with this paintable texture.</summary>
		public P3dPaintable CachedPaintable
		{
			get
			{
				if (cachedPaintableSet == false)
				{
					cachedPaintable    = GetComponent<P3dPaintable>();
					cachedPaintableSet = true;
				}

				return cachedPaintable;
			}
		}

		/// <summary>This gives you the current state of this paintabe texture.
		/// NOTE: This will only exist if your texture is activated.
		/// NOTE: This is a <b>RenderTexture</b>, so you can't directly read it. Use the <b>GetReadableCopy()</b> method if you need to.
		/// NOTE: This doesn't include any preview painting information, access the Preview property if you need to.</summary>
		public RenderTexture Current
		{
			set
			{
				if (materialSet == true)
				{
					current = value;

					material.SetTexture(slot.Name, current);
				}
			}

			get
			{
				return current;
			}
		}

		/// <summary>This gives you the current state of this paintabe texture including any preview painting information.</summary>
		public RenderTexture Preview
		{
			get
			{
				return preview;
			}
		}

		/// <summary>This allows you to get a list of all paintable textures on a P3dModel/P3dPaintable with the specified group.</summary>
		public static List<P3dPaintableTexture> Filter(P3dModel model, P3dGroup group)
		{
			tempPaintableTextures.Clear();

			if (model.Paintable != null)
			{
				var paintableTextures = model.Paintable.PaintableTextures;

				for (var i = paintableTextures.Count - 1; i >= 0; i--)
				{
					var paintableTexture = paintableTextures[i];

					if (paintableTexture.group == group)
					{
						tempPaintableTextures.Add(paintableTexture);
					}
				}
			}

			return tempPaintableTextures;
		}

		/// <summary>This will clear all undo/redo texture states.</summary>
		[ContextMenu("Clear States")]
		public void ClearStates()
		{
			if (paintableStates != null)
			{
				for (var i = paintableStates.Count - 1; i >= 0; i--)
				{
					paintableStates[i].Pool();
				}

				paintableStates.Clear();

				stateIndex = 0;
			}
		}

		/// <summary>This will store a texture state so that it can later be undone. This should be called before you perform texture modifications.</summary>
		[ContextMenu("Store State")]
		public void StoreState()
		{
			if (activated == true)
			{
				// If this is the latest state, then don't store or trim future
				if (stateIndex != paintableStates.Count - 1)
				{
					TrimFuture();

					AddState();
				}

				if (state == StateType.FullTextureCopy)
				{
					TrimPast();
				}

				stateIndex = paintableStates.Count;
			}
		}

		/// <summary>This will revert the texture to a previous state, if you have an undo state stored.</summary>
		[ContextMenu("Undo")]
		public void Undo()
		{
			if (CanUndo == true)
			{
				// If we're undoing for the first time, store the current state so we can redo back to it
				if (stateIndex == paintableStates.Count)
				{
					AddState();
				}

				ClearCommands();

				stateIndex -= 1;

				switch (state)
				{
					case StateType.FullTextureCopy:
					{
						var paintableState = paintableStates[stateIndex];

						P3dHelper.Blit(current, paintableState.Texture);
					}
					break;

					case StateType.LocalCommandCopy:
					{
						RebuildFromCommands();
					}
					break;
				}

				NotifyOnModified(false);
			}
		}

		/// <summary>This will restore a previously undone texture state, if you've performed an undo.</summary>
		[ContextMenu("Redo")]
		public void Redo()
		{
			if (CanRedo == true)
			{
				ClearCommands();

				stateIndex += 1;

				switch (state)
				{
					case StateType.FullTextureCopy:
					{
						var paintableState = paintableStates[stateIndex];

						P3dHelper.Blit(current, paintableState.Texture);

						NotifyOnModified(false);
					}
					break;

					case StateType.LocalCommandCopy:
					{
						RebuildFromCommands();
					}
					break;
				}
			}
		}

		public Vector2 GetCoord(ref RaycastHit hit)
		{
			switch (coord)
			{
				case P3dCoord.First: return hit.textureCoord;
				case P3dCoord.Second: return hit.textureCoord2;
			}

			return default(Vector2);
		}

		private void RebuildFromCommands()
		{
			P3dCommandReplace.Blit(current, texture, color);

			var localToWorldMatrix = transform.localToWorldMatrix;

			for (var i = 0; i <= stateIndex; i++)
			{
				var paintableState = paintableStates[i];
				var commandCount   = paintableState.Commands.Count;

				for (var j = 0; j < commandCount; j++)
				{
					var worldCommand = paintableState.Commands[j].SpawnCopy();

					worldCommand.Transform(localToWorldMatrix, localToWorldMatrix);

					AddCommand(worldCommand);
				}
			}

			ExecuteCommands(false);

			NotifyOnModified(false);
		}

		private void AddState()
		{
			var paintableState = P3dPaintableState.Pop();

			switch (state)
			{
				case StateType.FullTextureCopy:
				{
					paintableState.Write(current);
				}
				break;

				case StateType.LocalCommandCopy:
				{
					paintableState.Write(localCommands);

					localCommands.Clear();
				}
				break;
			}

			paintableStates.Add(paintableState);
		}

		private void TrimFuture()
		{
			for (var i = paintableStates.Count - 1; i >= stateIndex; i--)
			{
				paintableStates[i].Pool();

				paintableStates.RemoveAt(i);
			}
		}

		private void TrimPast()
		{
			for (var i = paintableStates.Count - stateLimit - 1; i >= 0; i--)
			{
				paintableStates[i].Pool();

				paintableStates.RemoveAt(i);
			}
		}

		/// <summary>You should call this after painting this paintable texture.</summary>
		public void NotifyOnModified(bool preview)
		{
			if (OnModified != null)
			{
				OnModified.Invoke(preview);
			}
		}
		
		/// <summary>This method returns a <b>Texture2D</b> copy of the current texture state, allowing you to read pixel values, etc.
		/// NOTE: This method can be slow if your texture is large.
		/// NOTE: A new texture is allocated each time you call this, so you must manually delete it when finished.</summary>
		public Texture2D GetReadableCopy()
		{
			return P3dHelper.GetReadableCopy(current);
		}

		/// <summary>This method returns the current texture state as a PNG byte array.
		/// NOTE: This method can be slow if your texture is large.
		/// NOTE: This component must be activated.</summary>
		public byte[] GetPngData()
		{
			var tempTexture = GetReadableCopy();

			if (tempTexture != null)
			{
				var data = tempTexture.EncodeToPNG();

				P3dHelper.Destroy(tempTexture);

				return data;
			}

			return null;
		}

		/// <summary>This method will clear the current texture state with the current <b>Texture</b> and <b>Color</b> values.
		/// NOTE: This component must be activated, and this method will not resize the current texture.</summary>
		[ContextMenu("Clear")]
		public void Clear()
		{
			Clear(texture, color);
		}

		/// <summary>This method will clear the current texture state with the specified texture and color.
		/// NOTE: This component must be activated, and this method will not resize the current texture.</summary>
		public void Clear(Texture texture, Color tint)
		{
			if (activated == true)
			{
				P3dCommandReplace.Blit(current, texture, tint);
			}
		}

		/// <summary>This method will replace the current texture state with the current <b>Texture</b> and <b>Color</b> values, including size.
		/// NOTE: This component must be activated</summary>
		[ContextMenu("Replace")]
		public void Replace()
		{
			Replace(texture, color);
		}

		/// <summary>This method will resize the current texture state based on the specified texture, and then replace its contents with the specified texture and color.
		/// NOTE: This component must be activated.</summary>
		public void Replace(Texture texture, Color tint)
		{
			if (texture != null)
			{
				Resize(texture.width, texture.height, false);
			}
			else
			{
				Resize(width, height, false);
			}

			Clear(texture, tint);
		}

		/// <summary>This method will resize the current texture state with the specified width and height.
		/// NOTE: This component must be activated.</summary>
		public bool Resize(int width, int height, bool copyContents = true)
		{
			if (activated == true)
			{
				if (current.width != width || current.height != height)
				{
					var descriptor = current.descriptor;

					descriptor.width  = width;
					descriptor.height = height;

					var newCurrent = P3dHelper.GetRenderTexture(descriptor);

					if (copyContents == true)
					{
						P3dCommandReplace.Blit(newCurrent, current, Color.white);

						if (newCurrent.useMipMap == true)
						{
							newCurrent.GenerateMips();
						}
					}

					P3dHelper.ReleaseRenderTexture(current);

					current = newCurrent;

					return true;
				}
			}

			return false;
		}

		/// <summary>This method will save the current texture state to PlayerPrefs using the current <b>SaveName</b>.</summary>
		[ContextMenu("Save")]
		public void Save()
		{
			Save(saveName);
		}

		/// <summary>This will save the current texture state with the specified save name.</summary>
		public void Save(string saveName)
		{
			if (activated == true && string.IsNullOrEmpty(saveName) == false)
			{
				P3dHelper.SaveBytes(saveName, GetPngData());
			}
		}

		/// <summary>This method will replace the current texture state with the data saved at <b>SaveName</b>.</summary>
		[ContextMenu("Load")]
		public void Load()
		{
			Load(saveName);
		}

		/// <summary>This method will replace the current texture state with the data saved at the specified save name.</summary>
		public void Load(string saveName, bool replace = true)
		{
			if (activated == true)
			{
				LoadFromData(P3dHelper.LoadBytes(saveName));
			}
		}

		/// <summary>This method will replace the current texture state with the specified image data (e.g. png).</summary>
		public void LoadFromData(byte[] data, bool allowResize = true)
		{
			if (data != null && data.Length > 0)
			{
				var tempTexture = new Texture2D(1, 1, TextureFormat.ARGB32, false, QualitySettings.activeColorSpace == ColorSpace.Linear);

				tempTexture.LoadImage(data);

				if (allowResize == true)
				{
					Replace(tempTexture, Color.white);
				}
				else
				{
					Clear(tempTexture, Color.white);
				}

				P3dHelper.Destroy(tempTexture);
			}
		}

		/// <summary>If you last painted using preview painting and you want to hide the preview painting, you can call this method to force the texture to go back to its current state.</summary>
		public void HidePreview()
		{
			if (activated == true && current != null && material != null)
			{
				material.SetTexture(slot.Name, current);
			}
		}

		/// <summary>This automatically calls <b>HidePreview</b> on all active and enabled paintable textures.</summary>
		public static void HideAllPreviews()
		{
			var instance = FirstInstance;

			for (var i = 0; i < InstanceCount; i++)
			{
				instance.HidePreview();

				instance = instance.NextInstance;
			}
		}

		/// <summary>This will clear save data with the current <b>SaveName</b>.</summary>
		[ContextMenu("Clear Save")]
		public void ClearSave()
		{
			P3dHelper.ClearSave(saveName);
		}

		/// <summary>This will clear save data with the specified save name.</summary>
		public static void ClearSave(string saveName)
		{
			P3dHelper.ClearSave(saveName);
		}

		/// <summary>If you modified the slot material index, then call this to update the cached material.</summary>
		[ContextMenu("Update Material")]
		public void UpdateMaterial()
		{
			material    = P3dHelper.GetMaterial(gameObject, slot.Index);
			materialSet = true;
		}

		/// <summary>If the current slot has a texture, this allows you to copy the width and height from it.</summary>
		[ContextMenu("Copy Size")]
		public void CopySize()
		{
			var texture = Slot.FindTexture(gameObject);

			if (texture != null)
			{
				width  = texture.width;
				height = texture.height;
			}
		}

		/// <summary>This copies the texture from the current slot.</summary>
		[ContextMenu("Copy Texture")]
		public void CopyTexture()
		{
			Texture = Slot.FindTexture(gameObject);
		}

		/// <summary>This allows you to manually activate this paintable texture.
		/// NOTE: This will automatically be called by the associated P3dPaintable component when it activates.</summary>
		[ContextMenu("Activate")]
		public void Activate()
		{
			if (activated == false)
			{
				UpdateMaterial();

				if (material != null)
				{
					var finalWidth   = width;
					var finalHeight  = height;
					var finalTexture = material.GetTexture(slot.Name);

					CachedPaintable.ScaleSize(ref finalWidth, ref finalHeight);

					if (texture != null)
					{
						finalTexture = texture;
					}

					if (string.IsNullOrEmpty(shaderKeyword) == false)
					{
						material.EnableKeyword(shaderKeyword);
					}

					var desc = new RenderTextureDescriptor(width, height, format, 0);

					desc.autoGenerateMips = false;

					if (mipMaps == MipType.ForceOn)
					{
						desc.useMipMap = true;
					}
					else if (mipMaps == MipType.Auto && P3dHelper.HasMipMaps(finalTexture) == true)
					{
						desc.useMipMap = true;
					}

					current = P3dHelper.GetRenderTexture(desc);

					P3dCommandReplace.Blit(current, finalTexture, color);

					if (current.useMipMap == true) current.GenerateMips();

					material.SetTexture(slot.Name, current);

					activated = true;

					if (string.IsNullOrEmpty(saveName) == false)
					{
						Load();
					}

					NotifyOnModified(false);
				}
			}
		}

		protected override void OnEnable()
		{
			base.OnEnable();

			if (paintableSet == false)
			{
				paintable    = GetComponent<P3dPaintable>();
				paintableSet = true;
			}

			paintable.Register(this);
		}

		protected override void OnDisable()
		{
			base.OnDisable();
			
			paintable.Unregister(this);
		}

		protected virtual void OnDestroy()
		{
			if (activated == true)
			{
				if (string.IsNullOrEmpty(saveName) == false)
				{
					Save();
				}

				P3dHelper.ReleaseRenderTexture(current);
				P3dHelper.ReleaseRenderTexture(preview);

				ClearStates();
			}
		}

		/// <summary>This will add a paint command to this texture's paint stack. The paint stack will be executed at the end of the current frame.</summary>
		public void AddCommand(P3dCommand command)
		{
			if (command.Preview == true)
			{
				previewCommands.Add(command);
			}
			else
			{
				paintCommands.Add(command);

				if (state == StateType.LocalCommandCopy && command.Preview == false)
				{
					var localCommand = command.SpawnCopyLocal(transform);

					localCommands.Add(localCommand);
				}
			}

			if (OnAddCommand != null)
			{
				OnAddCommand(command);
			}
		}

		/// <summary>This lets you know if there are paint commands in this paintable texture's paint stack.</summary>
		public bool CommandsPending
		{
			get
			{
				return paintCommands.Count + previewCommands.Count > 0;
			}
		}

		public void ClearCommands()
		{
			for (var i = previewCommands.Count - 1; i >= 0; i--)
			{
				previewCommands[i].Pool();
			}

			previewCommands.Clear();

			for (var i = paintCommands.Count - 1; i >= 0; i--)
			{
				paintCommands[i].Pool();
			}

			paintCommands.Clear();

			for (var i = localCommands.Count - 1; i >= 0; i--)
			{
				localCommands[i].Pool();
			}

			localCommands.Clear();
		}

		/// <summary>This allows you to manually execute all commands in the paint stack.
		/// This is useful if you need to modify the state of your object before the end of the frame.</summary>
		public void ExecuteCommands(bool sendNotifications)
		{
			if (activated == true)
			{
				var hidePreview = true;

				if (CommandsPending == true)
				{
					var oldActive      = RenderTexture.active;
					var preparedMesh   = default(Mesh);
					var preparedMatrix = default(Matrix4x4);

					// Paint
					if (paintCommands.Count > 0)
					{
						ExecuteCommands(paintCommands, sendNotifications, ref current, ref preview, ref preparedMesh, ref preparedMatrix);
					}

					var swap = preview;

					preview = null;

					// Preview
					if (previewCommands.Count > 0)
					{
						preview = swap;
						swap    = null;

						if (preview == null)
						{
							preview = P3dHelper.GetRenderTexture(current.descriptor);
						}

						hidePreview = false;

						preview.DiscardContents();

						Graphics.Blit(current, preview);

						previewCommands.Sort(P3dCommand.Compare);

						ExecuteCommands(previewCommands, sendNotifications, ref preview, ref swap, ref preparedMesh, ref preparedMatrix);
					}

					P3dHelper.ReleaseRenderTexture(swap);

					RenderTexture.active = oldActive;
				}

				if (hidePreview == true)
				{
					preview = P3dHelper.ReleaseRenderTexture(preview);
				}

				if (materialSet == false)
				{
					UpdateMaterial();
				}

				material.SetTexture(slot.Name, preview != null ? preview : current);
			}
		}

		private void ExecuteCommands(List<P3dCommand> commands, bool sendNotifications, ref RenderTexture main, ref RenderTexture swap, ref Mesh preparedMesh, ref Matrix4x4 preparedMatrix)
		{
			if (swap == null)
			{
				swap = P3dHelper.GetRenderTexture(main.descriptor);
			}

			if (keepUnpaintable == true)
			{
				P3dCommandReplace.BlitFast(swap, current, Color.white);
			}

			RenderTexture.active = main;

			for (var i = 0; i < commands.Count; i++)
			{
				var command = commands[i];

				RenderTexture.active = swap;

				command.Apply(main);

				if (command.RequireMesh == true)
				{
					paintable.GetPrepared(ref preparedMesh, ref preparedMatrix);

					P3dHelper.Draw(command.Material, preparedMesh, preparedMatrix, slot.Index, coord);
				}
				else
				{
					P3dHelper.Draw(command.Material);
				}

				P3dHelper.Swap(ref main, ref swap);

				command.Pool();
			}

			commands.Clear();

			if (main.useMipMap == true)
			{
				main.GenerateMips();
			}

			if (sendNotifications == true)
			{
				NotifyOnModified(commands == previewCommands);
			}
		}
	}
}

#if UNITY_EDITOR
namespace PaintIn3D
{
	[CanEditMultipleObjects]
	[CustomEditor(typeof(P3dPaintableTexture))]
	public class P3dPaintableTexture_Editor : P3dEditor<P3dPaintableTexture>
	{
		private bool expandSlot;

		protected override void OnInspector()
		{
			if (Any(t => t.Activated == true))
			{
				EditorGUILayout.HelpBox("This component has been activated.", MessageType.Info);
			}

			DrawExpand(ref expandSlot, "slot", "The material index and shader texture slot name that this component will paint.");
			if (expandSlot == true)
			{
				BeginIndent();
					BeginDisabled();
						EditorGUI.ObjectField(P3dHelper.Reserve(), new GUIContent("Texture", "This is the current texture in the specified texture slot."), Target.Slot.FindTexture(Target.gameObject), typeof(Texture), false);
					EndDisabled();
				EndIndent();
			}
			Draw("coord", "The UV channel this texture is mapped to.");
			Draw("shaderKeyword", "Some shaders require specific shader keywords to be enabled when adding new textures. If there is no texture in your selected slot then you may need to set this keyword.");

			Separator();

			Draw("group", "The group you want to associate this texture with. Only painting components with a matching group can paint this texture. This allows you to paint multiple textures at the same time with different settings (e.g. Albedo + Normal).");
			Draw("state", "This allows you to set how this texture's state is stored. This allows you to perform undo and redo operations.\n\nFullTextureCopy = A full copy of your texture will be copied for each state. This allows you to quickly undo and redo, and works with animated skinned meshes, but it uses up a lot of texture memory.\n\nLocalCommandCopy = Each paint command will be stored in local space for each state. This allows you to perform unlimited undo and redo states with minimal memory usage, because the object will be repainted from scratch. However, performance will depend on how many states must be redrawn.");
			if (Any(t => t.State == P3dPaintableTexture.StateType.FullTextureCopy))
			{
				BeginIndent();
					Draw("stateLimit", "The amount of times this texture can have its paint operations undone.", "Limit");
				EndIndent();
			}
			Draw("saveName", "If you want this texture to automatically save/load, then you can set the unique save name for it here. Keep in mind this setting won't work properly with prefab spawning since all clones will share the same SaveName.");

			Separator();

			Draw("format", "The format of the created texture.");
			Draw("mipMaps", "The mip maps of the created texture.\n\nAuto = On or Off based on the <b>Texture</b> mip map count.\n\nForceOn = Always enabled.\n\nForceOff = Always disabled.");
			Draw("keepUnpaintable", "If you disable this, then the unpaintable areas of this texture be discarded, and thus improve painting performance.\n\nNOTE: This is on by default, because some effects may require them to be preserved.");

			DrawSize();
			DrawTexture();
			Draw("color", "When activated or cleared, this paintable texture will be given this color.\n\nNOTE: If Texture is set, then each pixel RGBA value will be multiplied/tinted by this color.");
		}

		private void DrawSize()
		{
			var rect  = P3dHelper.Reserve();
			var rectL = rect; rectL.width = EditorGUIUtility.labelWidth;

			EditorGUI.LabelField(rectL, new GUIContent("Size", "This allows you to control the width and height of the texture when it gets activated."));

			rect.xMin += EditorGUIUtility.labelWidth;

			var rectR = rect; rectR.xMin = rectR.xMax - 48;
			var rectW = rect; rectW.xMax -= 50; rectW.xMax -= rectW.width / 2 + 1;
			var rectH = rect; rectH.xMax -= 50; rectH.xMin += rectH.width / 2 + 1;

			EditorGUI.PropertyField(rectW, serializedObject.FindProperty("width"), GUIContent.none);
			EditorGUI.PropertyField(rectH, serializedObject.FindProperty("height"), GUIContent.none);

			BeginDisabled(All(CannotScale));
				if (GUI.Button(rectR, new GUIContent("Copy", "Copy the width and height from the current slot?"), EditorStyles.miniButton) == true)
				{
					Undo.RecordObjects(targets, "Copy Sizes");

					Each(t => t.CopySize(), true);
				}
			EndDisabled();
		}

		private void DrawTexture()
		{
			var rect  = P3dHelper.Reserve();
			var rectL = rect; rectL.xMax -= 50;
			var rectR = rect; rectR.xMin = rectR.xMax - 48;

			EditorGUI.PropertyField(rectL, serializedObject.FindProperty("texture"), new GUIContent("Texture", "When activated or cleared, this paintable texture will be given this texture, and then multiplied/tinted by the Color.\n\nNone = White."));

			BeginDisabled(All(CannotCopyTexture));
				if (GUI.Button(rectR, new GUIContent("Copy", "Copy the texture from the current slot?"), EditorStyles.miniButton) == true)
				{
					Undo.RecordObjects(targets, "Copy Textures");

					Each(t => t.CopyTexture(), true);
				}
			EndDisabled();
		}

		private bool CannotScale(P3dPaintableTexture paintableTexture)
		{
			var texture = paintableTexture.Slot.FindTexture(paintableTexture.gameObject);

			if (texture != null)
			{
				if (texture.width != paintableTexture.Width || texture.height != paintableTexture.Height)
				{
					return false;
				}
			}

			return true;
		}

		private bool CannotCopyTexture(P3dPaintableTexture paintableTexture)
		{
			var texture = paintableTexture.Slot.FindTexture(paintableTexture.gameObject);

			if (texture != null && texture != paintableTexture.Texture)
			{
				return false;
			}

			return true;
		}
	}
}
#endif