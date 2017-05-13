using System;
using UnityEngine;
using System.Text;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using CoreFoundation;

/// <summary>
/// 6 创建继承Text的自定义ExText类 图文并排核心
/// </summary>
//[ExecuteInEditMode]
public class ExText : Text, IPointerClickHandler
{

    /// <summary>
    /// 用正则取标签属性 名称-大小
    /// </summary>
    private static readonly Regex m_spriteTagRegex =
    //new Regex(@"<quad name=(.+?) size=(\d*\.?\d+%?) w=(\d*\.?\d+%?) h=(\d*\.?\d+%?)/>", RegexOptions.Singleline);
    new Regex(@"<quad (.+?) size=(\d*\.?\d+%?) (\d*\.?\d+%?)>", RegexOptions.Singleline);//size是富文本的关键字,不能去掉或缩写.
    /// <summary>
    /// 需要渲染的图片信息列表
    /// </summary>
    private List<ExSpriteInfo> listSprite;
    /// <summary>
    /// 图片资源
    /// </summary>
    private SpriteAsset m_spriteAsset;
    /// <summary>
    /// 标签的信息列表
    /// </summary>
    private List<SpriteTagInfo> listTagInfo = new List<SpriteTagInfo>();
    /// <summary>
    /// 图片渲染组件
    /// </summary>
    private SpriteGraphic m_spriteGraphic;
    /// <summary>
    /// CanvasRenderer
    /// </summary>
    private CanvasRenderer m_spriteCanvasRenderer;
    /// <summary>
    /// 是否使用了图片(即只使用超链接功能)
    /// </summary>
    private bool usePic = true;//当只需要超链接的时候 就不要graphic组件,usePic = false 
    private bool haveDynamicEmoji = false;//文本中是否有动图

    /// <summary>
    /// 初始化 
    /// </summary>
    protected override void OnEnable()
    {
        //在编辑器中，可能在最开始会出现一张图片，就是因为没有激活文本，在运行中是正常的。可根据需求在编辑器中选择激活...
        base.OnEnable();

        if (m_spriteGraphic == null)
            m_spriteGraphic = GetComponentInChildren<SpriteGraphic>();
        if (null != m_spriteGraphic)
        {
            if (m_spriteCanvasRenderer == null)
                m_spriteCanvasRenderer = m_spriteGraphic.GetComponentInChildren<CanvasRenderer>();
            m_spriteAsset = m_spriteGraphic.m_spriteAsset;
        }
        else usePic = false; //(优化1)
    }
    /// <summary>
    /// 在设置顶点时调用
    /// </summary>
    public override void SetVerticesDirty()
    {
        base.SetVerticesDirty();
        m_OutputText = GetOutputText();

        //解析标签属性
        foreach (Match match in m_spriteTagRegex.Matches(text))
        {
            SpriteTagInfo tempSpriteTag = new SpriteTagInfo();
            tempSpriteTag.name = match.Groups[1].Value;
            tempSpriteTag.index = match.Index;
            //print("match.Index" + match.Index);
            //tempSpriteTag.size = new Vector2(float.Parse(match.Groups[2].Value)*float.Parse(match.Groups[3].Value), float.Parse(match.Groups[2].Value)*float.Parse(match.Groups[4].Value));
            tempSpriteTag.size = new Vector2(float.Parse(match.Groups[2].Value), float.Parse(match.Groups[2].Value));
            tempSpriteTag.Length = match.Length;
            if (null != match.Groups[3])
                tempSpriteTag.count = int.Parse(match.Groups[3].Value);
            listTagInfo.Add(tempSpriteTag);
            if (tempSpriteTag.count > 1 && !haveDynamicEmoji)
                haveDynamicEmoji = true;//count=1或无 则为静态表情  (优化2)
        }
    }
    readonly UIVertex[] m_TempVerts = new UIVertex[4];
    List<UIVertex> vertsTemp1 = new List<UIVertex>();
    /// <summary>
    /// 绘制模型
    /// </summary>
    /// <param name="toFill"></param>
    protected override void OnPopulateMesh(VertexHelper toFill)
    {
        //  base.OnPopulateMesh(toFill);

        if (font == null)
            return;

        // We don't care if we the font Texture changes while we are doing our Update.
        // The end result of cachedTextGenerator will be valid for this instance.
        // Otherwise we can get issues like Case 619238.
        m_DisableFontTextureRebuiltCallback = true;

        Vector2 extents = rectTransform.rect.size;

        var settings = GetGenerationSettings(extents);
        cachedTextGenerator.Populate(text, settings);

        Rect inputRect = rectTransform.rect;

        // get the text alignment anchor point for the text in local space
        Vector2 textAnchorPivot = GetTextAnchorPivot(alignment);
        Vector2 refPoint = Vector2.zero;
        refPoint.x = (textAnchorPivot.x == 1 ? inputRect.xMax : inputRect.xMin);
        refPoint.y = (textAnchorPivot.y == 0 ? inputRect.yMin : inputRect.yMax);

        // Determine fraction of pixel to offset text mesh.
        Vector2 roundingOffset = PixelAdjustPoint(refPoint) - refPoint;

        // Apply the offset to the vertices
        IList<UIVertex> verts = cachedTextGenerator.verts;
        float unitsPerPixel = 1 / pixelsPerUnit;
        //Last 4 verts are always a new line...
        int vertCount = verts.Count - 4;

        toFill.Clear();

        for (int i = 0; i < listTagInfo.Count; i++)
        {
            //UGUIText不能很好支持<quad/>标签，表现为乱码，我这里将他的uv全设置为0,清除乱码
            for (int m = listTagInfo[i].index * 4; m < listTagInfo[i].index * 4 + 4; m++)
            {
                UIVertex tempVertex = verts[m];
                tempVertex.uv0 = Vector2.zero;
                verts[m] = tempVertex;
            }
        }

        if (roundingOffset != Vector2.zero)
        {
            for (int i = 0; i < vertCount; ++i)
            {
                int tempVertsIndex = i & 3;
                m_TempVerts[tempVertsIndex] = verts[i];
                m_TempVerts[tempVertsIndex].position *= unitsPerPixel;
                m_TempVerts[tempVertsIndex].position.x += roundingOffset.x;
                m_TempVerts[tempVertsIndex].position.y += roundingOffset.y;
                if (tempVertsIndex == 3)
                    toFill.AddUIVertexQuad(m_TempVerts);
            }
        }
        else
        {
            for (int i = 0; i < vertCount; ++i)
            {
                int tempVertsIndex = i & 3;
                m_TempVerts[tempVertsIndex] = verts[i];
                m_TempVerts[tempVertsIndex].position *= unitsPerPixel;
                if (tempVertsIndex == 3)
                    toFill.AddUIVertexQuad(m_TempVerts);
            }
        }

        //计算标签 计算偏移值后 再计算标签的值
        List<UIVertex> vertsTemp = new List<UIVertex>();
        for (int i = 0; i < vertCount; i++)
        {
            UIVertex tempVer = new UIVertex();
            toFill.PopulateUIVertex(ref tempVer, i);
            vertsTemp.Add(tempVer);
        }

        if(usePic)
        CalcQuadTag(vertsTemp);//解析quad标签  主要清除quad乱码 获取表情的位置

        vertsTemp1 = vertsTemp;
        m_DisableFontTextureRebuiltCallback = false;

        var orignText = m_Text;
        m_Text = m_OutputText;
        base.OnPopulateMesh(toFill);
        m_Text = orignText;

        UIVertex vert = new UIVertex();
        // 处理超链接包围框
        foreach (var hrefInfo in m_HrefInfos)
        {
            hrefInfo.boxes.Clear();
            if (hrefInfo.startIndex >= toFill.currentVertCount)
            {
                continue;
            }

            // 将超链接里面的文本顶点索引坐标加入到包围框
            toFill.PopulateUIVertex(ref vert, hrefInfo.startIndex);
            var pos = vert.position;
            var bounds = new Bounds(pos, Vector3.zero);
            for (int i = hrefInfo.startIndex, m = hrefInfo.endIndex; i < m; i++)
            {
                if (i >= toFill.currentVertCount)
                {
                    break;
                }

                toFill.PopulateUIVertex(ref vert, i);
                pos = vert.position;
                if (pos.x < bounds.min.x) // 换行重新添加包围框
                {
                    hrefInfo.boxes.Add(new Rect(bounds.min, bounds.size));
                    bounds = new Bounds(pos, Vector3.zero);
                }
                else
                {
                    bounds.Encapsulate(pos); // 扩展包围框
                }
            }
            hrefInfo.boxes.Add(new Rect(bounds.min, bounds.size));
        }

        if(usePic)
        //绘制图片
        DrawSprite();
    }

    Rect spriteRect = new Rect();
    List<ExSpriteInfo> tempSprites = new List<ExSpriteInfo>();
    /// <summary>
    /// 解析quad标签  主要清除quad乱码 获取表情的位置
    /// </summary>
    /// <param name="verts"></param>
    void CalcQuadTag(IList<UIVertex> verts)
    {
        //通过标签信息来设置需要绘制的图片的信息
        if (null == listSprite)
            listSprite = new List<ExSpriteInfo>();
        else
            listSprite.Clear();
        for (int i = 0; i < listTagInfo.Count; i++)
        {
            ExSpriteInfo tempSprite = new ExSpriteInfo();
            // tempSprite = new ExSpriteInfo();

            //获取表情的第一个位置,则计算他的位置为quad占位的第四个点   顶点绘制顺序:       
            //                                                                       0    1
            //                                                                       3    2
            SpriteTagInfo listTagInfo_i = listTagInfo[i];
            tempSprite.textpos = verts[((listTagInfo_i.index + 1) * 4) - 1].position;
            //Debug.Log("listTagInfo_i.index "+ listTagInfo_i.index + "listTagInfo.Count " + listTagInfo.Count);
            //设置图片的位置
            tempSprite.vertices = new Vector3[4];
            tempSprite.vertices[0] = new Vector3(0, 0, 0) + tempSprite.textpos;
            tempSprite.vertices[1] = new Vector3(listTagInfo_i.size.x, listTagInfo_i.size.y, 0) + tempSprite.textpos;
            tempSprite.vertices[2] = new Vector3(listTagInfo_i.size.x, 0, 0) + tempSprite.textpos;
            tempSprite.vertices[3] = new Vector3(0, listTagInfo_i.size.y, 0) + tempSprite.textpos;
            tempSprites.Add(tempSprite);
            //计算其uv  //spriteRect只是需要一个uv模板,具体是谁的并不关心
            //spriteRect = m_spriteAsset.listSpriteInfo[0].rect;
            for (int j = 0; j < m_spriteAsset.listSpriteInfo.Count; j++)
            {
                //通过标签的名称去索引spriteAsset里所对应的sprite的名称
                if (listTagInfo_i.name == m_spriteAsset.listSpriteInfo[j].name)//这里用emoji的名称判断,然后记录ID,供后面用ID对比更新.(之所以不直接用ID判断是为了,可以通过发送#+名称 来发送表情) (优化3)
                {
                    listTagInfo_i.ID = m_spriteAsset.listSpriteInfo[j].ID;
                    spriteRect = m_spriteAsset.listSpriteInfo[j].rect;
                }
                Vector2 texSize = new Vector2(m_spriteAsset.texSource.width, m_spriteAsset.texSource.height);
                tempSprite.uv = new Vector2[4];
                tempSprite.uv[0] = new Vector2(spriteRect.x / texSize.x, spriteRect.y / texSize.y);
                tempSprite.uv[1] = new Vector2((spriteRect.x + spriteRect.width) / texSize.x, (spriteRect.y + spriteRect.height) / texSize.y);
                tempSprite.uv[2] = new Vector2((spriteRect.x + spriteRect.width) / texSize.x, spriteRect.y / texSize.y);
                tempSprite.uv[3] = new Vector2(spriteRect.x / texSize.x, (spriteRect.y + spriteRect.height) / texSize.y);

                //声明三角顶点所需要的数组
                tempSprite.triangles = new int[6];
                listSprite.Add(tempSprite);
            }
        }
    }

    SpriteTagInfo listTagInfo_i;
    /// <summary>
    /// 更新动图 :将需要动图更新的地方,单独提出来更新(不变的就不更新)(优化4)
    /// </summary>
    void ChangeSprites()
    {
        listSprite.Clear();//容器要清空,性能巨提升(优化5)
        int listTagInfo_Count = listTagInfo.Count;//List.Count  循环的话 消耗也比较大,所以做个函数内局部缓存(优化6)
        int spriteAsset_ListSpriteInfo_Count = m_spriteAsset.listSpriteInfo.Count;
        for (int i = 0; i < listTagInfo_Count; i++)
        {
            listTagInfo_i = listTagInfo[i];
            for (int j = 0; j < spriteAsset_ListSpriteInfo_Count; j++)
            {
                //通过标签的名称去索引spriteAsset里所对应的sprite的名称
                if (listTagInfo[i].ID == m_spriteAsset.listSpriteInfo[j].ID)//这儿用字符串比较,消耗比较大 所以换做ID来判断
                {
                    if (listTagInfo_i.count >= emojiId)
                    {
                        spriteRect = m_spriteAsset.listSpriteInfo[emojiId + m_spriteAsset.listSpriteInfo[j].ID].rect;
                    }
                    else
                    {
                        spriteRect = m_spriteAsset.listSpriteInfo[listTagInfo_i.count + m_spriteAsset.listSpriteInfo[j].ID].rect;
                    }
                }
            }
            Vector2 texSize = new Vector2(m_spriteAsset.texSource.width, m_spriteAsset.texSource.height);
            ExSpriteInfo tempSprite = tempSprites[i];
            tempSprite.uv = new Vector2[4];
            tempSprite.uv[0] = new Vector2(spriteRect.x / texSize.x, spriteRect.y / texSize.y);
            tempSprite.uv[1] = new Vector2((spriteRect.x + spriteRect.width) / texSize.x, (spriteRect.y + spriteRect.height) / texSize.y);
            tempSprite.uv[2] = new Vector2((spriteRect.x + spriteRect.width) / texSize.x, spriteRect.y / texSize.y);
            tempSprite.uv[3] = new Vector2(spriteRect.x / texSize.x, (spriteRect.y + spriteRect.height) / texSize.y);

            listSprite.Add(tempSprite);
        }
    }

    /// <summary>
    /// 动图最大张数
    /// </summary>
    private int max_count = 4;
    private int emojiId = 0;
    private float gapTime =0.4f;
    /// <summary>
    /// 动图迭代器
    /// </summary>
    /// <returns></returns>
    IEnumerator RefreshEmojis()
    {
        yield return new WaitForSeconds(gapTime);
        ChangeSprites();            //改变动图纹理
        DrawSprite();               //重新绘制精灵

        emojiId++;
        if (emojiId >= max_count)
        { emojiId = 0; }
    }

    //集合在循环外申请,循环内使用前清空即可 减少构造初始化消耗 (优化7)
    List<Vector3> tempVertices = new List<Vector3>();
    List<Vector2> tempUv = new List<Vector2>();
    List<int> tempTriangles = new List<int>();
    /// <summary>
    /// 绘制图片
    /// </summary>
    void DrawSprite()
    {
        tempVertices.Clear();
        tempUv.Clear();
        tempTriangles.Clear();

        for (int i = 0; i < listSprite.Count; i++)
        {
            for (int j = 0; j < listSprite[i].vertices.Length; j++)
            {
                tempVertices.Add(listSprite[i].vertices[j]);
            }
            for (int j = 0; j < listSprite[i].uv.Length; j++)
            {
                tempUv.Add(listSprite[i].uv[j]);
            }
            for (int j = 0; j < listSprite[i].triangles.Length; j++)
            {
                tempTriangles.Add(listSprite[i].triangles[j]);
            }
        }
        //计算顶点绘制顺序
        for (int i = 0; i < tempTriangles.Count; i++)
        {
            if (i % 6 == 0)
            {
                int num = i / 6;
                tempTriangles[i] = 0 + 4 * num;
                tempTriangles[i + 1] = 1 + 4 * num;
                tempTriangles[i + 2] = 2 + 4 * num;

                tempTriangles[i + 3] = 1 + 4 * num;
                tempTriangles[i + 4] = 0 + 4 * num;
                tempTriangles[i + 5] = 3 + 4 * num;
            }
        }
        Mesh m_spriteMesh = new Mesh();
        m_spriteMesh.vertices = tempVertices.ToArray();
        m_spriteMesh.uv = tempUv.ToArray();
        m_spriteMesh.triangles = tempTriangles.ToArray();
        if (m_spriteMesh == null)
            return;
        if (usePic)
        {
            m_spriteCanvasRenderer.SetMesh(m_spriteMesh);
            m_spriteGraphic.UpdateMaterial();
        }
        //print("haveDynamicEmoji " + haveDynamicEmoji);
        if (usePic && haveDynamicEmoji)
        {
            StartCoroutine(RefreshEmojis());
        }
    }


    /// <summary>
    /// 解析完最终的文本
    /// </summary>
    private string m_OutputText;

    /// <summary>
    /// 超链接信息列表
    /// </summary>
    private readonly List<HrefInfo> m_HrefInfos = new List<HrefInfo>();

    /// <summary>
    /// 文本构造器
    /// </summary>
    private static readonly StringBuilder s_TextBuilder = new StringBuilder();

    /// <summary>
    /// 超链接正则
    /// </summary>
    private static readonly Regex s_HrefRegex =
        new Regex(@"<a ([^>\n\s]+)>(.*?)(</a>)", RegexOptions.Singleline);

    public class HrefClickEvent : UnityEvent<string> { }

    [SerializeField]
    private HrefClickEvent m_OnHrefClick = new HrefClickEvent();

    /// <summary>
    /// 超链接点击事件
    /// </summary>
    public HrefClickEvent onHrefClick
    {
        get { return m_OnHrefClick;}
        set { m_OnHrefClick = value; }
    }

    //System.Action<string> onHrefClick;

    /// <summary>
    /// 获取超链接解析后的最后输出文本
    /// </summary>
    /// <returns></returns>
    protected string GetOutputText()
    {
        s_TextBuilder.Length = 0;
        m_HrefInfos.Clear();
        var indexText = 0;
        foreach (Match match in s_HrefRegex.Matches(text))
        {
            s_TextBuilder.Append(text.Substring(indexText, match.Index - indexText));
            s_TextBuilder.Append("<color=yellow>");  // 超链接颜色

            var group = match.Groups[1];
            var hrefInfo = new HrefInfo
            {
                startIndex = s_TextBuilder.Length * 4, // 超链接里的文本起始顶点索引
                endIndex = (s_TextBuilder.Length + match.Groups[2].Length - 1) * 4 + 3,
                name = group.Value
            };
            m_HrefInfos.Add(hrefInfo);

            s_TextBuilder.Append(match.Groups[2].Value);
            s_TextBuilder.Append("</color>");
            indexText = match.Index + match.Length;
        }
        s_TextBuilder.Append(text.Substring(indexText, text.Length - indexText));
        return s_TextBuilder.ToString();
    }

    /// <summary>
    /// 点击事件检测是否点击到超链接文本
    /// </summary>
    /// <param name="eventData"></param>
    public virtual void OnPointerClick(PointerEventData eventData)
    {
        Vector2 lp;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform, eventData.position, eventData.pressEventCamera, out lp);

        foreach (var hrefInfo in m_HrefInfos)
        {
            var boxes = hrefInfo.boxes;
            for (var i = 0; i < boxes.Count; ++i)
            {
                if (boxes[i].Contains(lp))
                {
                    m_OnHrefClick.Invoke(hrefInfo.name);
                    return;
                }
            }
        }
    }

    /// <summary>
    /// 超链接信息类
    /// </summary>
    private class HrefInfo
    {
        public int startIndex;

        public int endIndex;

        public string name;

        public readonly List<Rect> boxes = new List<Rect>();
    }


}

/// <summary>
/// 精灵标签信息
/// </summary>
[System.Serializable]
public class SpriteTagInfo
{
    public int ID;
    /// <summary>
    /// sprite名称
    /// </summary>
    public string name;
    /// <summary>
    /// 对应的字符索引
    /// </summary>
    public int index;
    /// <summary>
    /// 大小
    /// </summary>
    public Vector2 size = new Vector2(33,33);

    public int Length;

    public int count=1;
}

/// <summary>
/// 文本信息
/// </summary>
[System.Serializable]
public class ExSpriteInfo
{
    // 文字的最后的位置
    public Vector3 textpos;
    // 4 顶点 
    public Vector3[] vertices;
    //4 uv
    public Vector2[] uv;
    //6 三角顶点顺序
    public int[] triangles;
}
