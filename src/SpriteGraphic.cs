using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
/// <summary>
/// 5 创建渲染组件
/// </summary>
public class SpriteGraphic : MaskableGraphic
{
    public SpriteAsset m_spriteAsset;
    //public List<SpriteAsset> m_spriteAsset = new List<SpriteAsset>();

    public override Texture mainTexture
    {
        get
        {
            if (m_spriteAsset == null)
                return s_WhiteTexture;

            if (m_spriteAsset.texSource == null)
                return s_WhiteTexture;
            else
                return m_spriteAsset.texSource;
        }
    }
    protected override void OnEnable()
    {
        //不调用父类的OnEnable 他默认会渲染整张图片
        //base.OnEnable();  
    }

#if UNITY_EDITOR
    //在编辑器下  
    protected override void OnValidate()
    {
        //base.OnValidate();//OnValidate使其无效
    }
#endif
    /// <summary>
    /// 尺寸改变的时候渲染(不能去掉此函数重载)
    /// </summary>
    protected override void OnRectTransformDimensionsChange()
    {
        // base.OnRectTransformDimensionsChange();
    }

    /// <summary>
    /// 绘制后 需要更新材质
    /// </summary>
    public new void UpdateMaterial()
    {
        base.UpdateMaterial();
    }

}
