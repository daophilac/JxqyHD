﻿using Engine.Gui.Base;
using Engine.ListManager;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Texture = Engine.Gui.Base.Texture;

namespace Engine.Gui
{
    public sealed class GoodsGui : GuiItem
    {
        private ListView _listView;
        private TextGui _money;
        private Texture2D _coldTimeBackground;
        private Color _colodTimeFontColor = Color.White;

        public static void DropHandler(object arg1, DragDropItem.DropEvent arg2)
        {
            var item = (DragDropItem)arg1;
            var sourceItem = arg2.Source;
            var data = item.Data as GoodItemData;
            var sourceData = sourceItem.Data as GoodItemData;
            if (data != null && sourceData != null)
            {
                if (GoodsListManager.IsInEquipRange(sourceData.Index))
                {
                    var info = GoodsListManager.GetItemInfo(data.Index);
                    var sourceGood = GoodsListManager.Get(sourceData.Index);
                    if (sourceGood == null ||
                        (info != null && info.TheGood == null) ||
                        (info != null && info.TheGood.Part != sourceGood.Part))
                    {
                        return;
                    }
                }
            }

            int index, sourceIndex;
            ExchangeItem(arg1, arg2, out index, out sourceIndex);
        }

        public static void MouseStayOverHandler(object arg1, GuiItem.MouseEvent arg2)
        {
            var item = (DragDropItem)arg1;
            var data = item.Data as GoodItemData;
            if (data != null)
            {
                var info = GoodsListManager.GetItemInfo(data.Index);
                if (info != null)
                    GuiManager.ToolTipInterface.ShowGood(info.TheGood, GuiManager.BuyInterface.IsShow);
            }
        }

        public static void MouseLeaveHandler(object arg1, GuiItem.MouseEvent arg2)
        {
            GuiManager.ToolTipInterface.IsShow = false;
        }

        public static void RightClickHandler(object arg1, GuiItem.MouseRightClickEvent arg2)
        {
            var theItem = (DragDropItem)arg1;
            var data = theItem.Data as GoodItemData;
            if (data == null) return;
            var good = GoodsListManager.Get(data.Index);
            if (good == null) return;

            if (GuiManager.BuyInterface.IsShow)
            {
                if (good.SellPrice.GetMaxValue() > 0)
                {
                    if (GuiManager.BuyInterface.CanSellSelfGoods)
                    {
                        Globals.ThePlayer.AddMoneyValue(good.SellPrice.GetMaxValue());
                        GuiManager.DeleteGood(good.FileName);
                        GuiManager.BuyInterface.AddGood(good);
                    }
                    else
                    {
                        GuiManager.ShowMessage("当前只能买物品");
                    }
                }
            }
            else
            {
                GoodsListManager.UsingGood(data.Index);
            }
        }

        public static bool ExchangeItem(object arg1, DragDropItem.DropEvent arg2, 
            out int index, out int sourceIndex)
        {
            var item = (DragDropItem)arg1;
            var sourceItem = arg2.Source;
            var data = item.Data as GoodItemData;
            var sourceData = sourceItem.Data as GoodItemData;
            if (data != null && sourceData != null)
            {
                GoodsListManager.ExchangeListItemAndEquiping(data.Index, sourceData.Index);
                item.BaseTexture = GoodsListManager.GetTexture(data.Index);
                sourceItem.BaseTexture = GoodsListManager.GetTexture(sourceData.Index);
                index = data.Index;
                sourceIndex = sourceData.Index;
                var info = GoodsListManager.GetItemInfo(index);
                var sourceInfo = GoodsListManager.GetItemInfo(sourceIndex);
                item.TopLeftText = info != null ? info.Count.ToString() : "";
                sourceItem.TopLeftText = sourceInfo != null ? sourceInfo.Count.ToString() : "";
                return true;
            }
            index = 0;
            sourceIndex = 0;
            return false;
        }

        public GoodsGui()
        {
            var cfg = GuiManager.Setttings.Sections["Goods"];
            var baseTexture = new Texture(Utils.GetAsf(null, cfg["Image"]));
            var position = new Vector2(
                Globals.WindowWidth / 2f + int.Parse(cfg["LeftAdjust"]),
                0f + int.Parse(cfg["TopAdjust"]));
            _listView = new ListView(null,
                position,
                new Vector2(int.Parse(cfg["ScrollBarLeft"]), int.Parse(cfg["ScrollBarRight"])),
                baseTexture.Width,
                baseTexture.Height,
                baseTexture,
                (GoodsListManager.StoreIndexEnd - GoodsListManager.StoreIndexBegin + 1 + 2)/3,
                GuiManager.Setttings.Sections["Goods_List_Items"],
                int.Parse(cfg["ScrollBarWidth"]),
                int.Parse(cfg["ScrollBarHeight"]),
                cfg["ScrollBarButton"],
                GoodsListManager.Type != GoodsListManager.ListType.TypeByGoodItem);
            _listView.Scrolled += delegate
            {
                UpdateItems();
            };
            _listView.RegisterItemDragHandler((arg1, arg2) =>
            {
                
            });
            _listView.RegisterItemDropHandler(DropHandler);
            _listView.RegisterItemMouseRightClickeHandler(RightClickHandler);
            _listView.RegisterItemMouseStayOverHandler(MouseStayOverHandler);
            _listView.RegisterItemMouseLeaveHandler(MouseLeaveHandler);

            cfg = GuiManager.Setttings.Sections["Goods_Money"];
            _money = new TextGui(_listView,
                new Vector2(int.Parse(cfg["Left"]), int.Parse(cfg["Top"])),
                int.Parse(cfg["Width"]),
                int.Parse(cfg["Height"]),
                Globals.FontSize7,
                0,
                0,
                "",
                Utils.GetColor(cfg["Color"]));

            IsShow = false;
        }

        public bool IsItemShow(int listIndex, out int index)
        {
            return _listView.IsItemShow(listIndex, out index);
        }

        public void UpdateItems()
        {
            for (var i = 0; i < 9; i++)
            {
                var index = _listView.ToListIndex(i) + GoodsListManager.StoreIndexBegin - 1;
                var info = GoodsListManager.GetItemInfo(index);
                Good good = null;
                if (info != null) good = info.TheGood;
                var image = (good == null ? null : good.Image);
                _listView.SetListItem(i, new Texture(image), new GoodItemData(index));
                _listView.SetItemTopLeftText(i, info != null ? info.Count.ToString() : "");
            }
        }

        public void UpdateListItem(int listIndex)
        {
            int itemIndex;
            if (IsItemShow(listIndex, out itemIndex))
            {
                _listView.SetListItemTexture(itemIndex,
                    GoodsListManager.GetTexture(listIndex));
                var info = GoodsListManager.GetItemInfo(listIndex);
                _listView.SetItemTopLeftText(itemIndex, info != null ? info.Count.ToString() : "");
            }
        }

        public override void Update(GameTime gameTime)
        {
            if (!IsShow) return;

            _listView.Update(gameTime);
            if (Globals.ThePlayer != null)
            {
                _money.Text = Globals.ThePlayer.Money.ToString();
            }
            else
            {
                _money.Text = "";
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!IsShow) return;

            _listView.Draw(spriteBatch);
            _money.Draw(spriteBatch);

            for (var i = 0; i < 9; i++)
            {
                var index = _listView.ToListIndex(i) + GoodsListManager.StoreIndexBegin - 1;
                var info = GoodsListManager.GetItemInfo(index);
                if(info != null && info.RemainColdMilliseconds > 0)
                {
                    var item = _listView.GetItem(i);
                    if (_coldTimeBackground == null)
                    {
                        _coldTimeBackground = TextureGenerator.GetColorTexture(new Color(0, 0, 0, 180), item.Width,
                            item.Height);
                    }

                    var timeTxt = (info.RemainColdMilliseconds / 1000f).ToString("0.0");
                    var font = Globals.FontSize10;

                    spriteBatch.Draw(
                     _coldTimeBackground,
                     item.ScreenPosition,
                     Color.White);

                    spriteBatch.DrawString(font,
                    timeTxt,
                    item.CenterScreenPosition - font.MeasureString(timeTxt) / 2,
                    _colodTimeFontColor);
                }
            }
        }

        public class GoodItemData
        {
            public int Index { private set; get; }
            public GoodItemData(int index)
            {
                Index = index;
            }
        }
    }
}