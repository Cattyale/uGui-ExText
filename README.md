# uGui-ExText
示例：
![image](http://github.com/Cattyale/uGui-ExText/Example.png)
Unity3d UGUI 动态表情，文本超链接
---------------------------说明文档---------------------------
------------------------重要参数方法-------------------------
max_count                  //动图最大张数
gapTime                      //刷新帧速
m_spriteTagRegex      //动态表情正则表达
s_HrefRegex                //超文本正则表达
onHrefClick                //超文本事件

---------------------------文本示例---------------------------
动态表情---------------"<quad 0 size=100 0>"
0:表情id
100:表情大小
0:表情图片张数(0为静态表情)
超文本链接-------------"<a 0>[装备]</a>"
0:超文本id

---------------------------实现方法---------------------------
动态表情包:
1.带有组件ExText,SpriteGraphic
2.XXX.asset挂脚本SpirtAsset
3.XXX.asset设置TexSource和ListSpriteInfo
4.TexSource为图集,ListSpriteInfo为每张图片设置
5.SpriteGraphic组件挂资源XXX.asset
超文本实现:
1.带有组件ExText
2.extext.onHrefClick.AddListener(Onclick);
3.Onclick方法只有一个参数,string
