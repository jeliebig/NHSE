﻿using System;
using System.Collections.Generic;
using System.Windows.Forms;
using NHSE.Core;
using NHSE.Sprites;

namespace NHSE.WinForms
{
    public partial class ItemGridEditor : UserControl
    {
        private static readonly ItemSpriteDrawer Sprites = SpriteUtil.Items;
        private readonly ItemEditor Editor;
        private readonly IReadOnlyList<Item> Items;
        private readonly string[] ItemNames;

        private IList<PictureBox> SlotPictureBoxes = Array.Empty<PictureBox>();
        private int Count => Items.Count;
        private int Page;
        private int ItemsPerPage;

        public ItemGridEditor(ItemEditor editor, IReadOnlyList<Item> items, string[] itemnames)
        {
            Editor = editor;
            Items = items;
            ItemNames = itemnames;
            InitializeComponent();

            HoverItem(Item.NO_ITEM);
        }

        public void InitializeGrid(int width, int height)
        {
            ItemsPerPage = width * height;
            ItemGrid.InitializeGrid(width, height, Sprites);
            InitializeSlots();
        }

        private void InitializeSlots()
        {
            SlotPictureBoxes = ItemGrid.Entries;
            foreach (var pb in SlotPictureBoxes)
            {
                pb.MouseEnter += Slot_MouseEnter;
                pb.MouseLeave += Slot_MouseLeave;
                pb.MouseClick += Slot_MouseClick;
                pb.MouseWheel += Slot_MouseWheel;
                pb.ContextMenuStrip = CM_Hand;
            }
            ChangePage();
        }

        private void Slot_MouseWheel(object sender, MouseEventArgs e)
        {
            var delta = e.Delta < 0 ? 1 : -1; // scrolling down increases page #
            var newpage = Math.Min(PageCount - 1, Math.Max(0, Page + delta));
            if (newpage == Page)
                return;
            Page = newpage;
            ChangePage();
        }

        public void Slot_MouseEnter(object? sender, EventArgs e)
        {
            if (!(sender is PictureBox pb))
                return;
            var index = SlotPictureBoxes.IndexOf(pb);
            HoverItem(index);
            pb.Image = Sprites.HoverBackground;
        }

        public void Slot_MouseLeave(object? sender, EventArgs e)
        {
            if (!(sender is PictureBox pb))
                return;
            pb.Image = null;
        }

        public void Slot_MouseClick(object sender, MouseEventArgs e)
        {
            switch (ModifierKeys)
            {
                case Keys.Control | Keys.Alt: ClickClone(sender, e); break;
                case Keys.Control: ClickView(sender, e); break;
                case Keys.Shift: ClickSet(sender, e); break;
                case Keys.Alt: ClickDelete(sender, e); break;
                default:
                    return;
            }
            // restart hovering since the mouse event isn't fired
            Slot_MouseEnter(sender, e);
        }

        public Item HoverItem(int index) => HoverItem(GetItem(index));
        public Item LoadItem(int index) => Editor.LoadItem(GetItem(index));
        public Item SetItem(int index) => Editor.SetItem(GetItem(index));

        private Item HoverItem(Item item)
        {
            L_ItemName.Text = ItemNames[item.ItemId == Item.NONE ? 0 : item.ItemId];
            return item;
        }

        private Item GetItem(int index)
        {
            index += Page * ItemsPerPage;
            return Items[index];
        }

        private void ClickView(object sender, EventArgs e)
        {
            var pb = WinFormsUtil.GetUnderlyingControl<PictureBox>(sender);
            if (pb == null)
                return;
            var index = SlotPictureBoxes.IndexOf(pb);
            LoadItem(index);
        }

        private void ClickSet(object sender, EventArgs e)
        {
            var pb = WinFormsUtil.GetUnderlyingControl<PictureBox>(sender);
            if (pb == null)
                return;
            var index = SlotPictureBoxes.IndexOf(pb);
            var item = SetItem(index);
            pb.BackgroundImage = Sprites.GetImage(item, L_ItemName.Font);
        }

        private void ClickDelete(object sender, EventArgs e)
        {
            var pb = WinFormsUtil.GetUnderlyingControl<PictureBox>(sender);
            if (pb == null)
                return;
            var index = SlotPictureBoxes.IndexOf(pb);
            var item = GetItem(index);
            item.Delete();
            pb.BackgroundImage = Sprites.GetImage(item, L_ItemName.Font);
        }

        private void ClickClone(object sender, EventArgs e)
        {
            var pb = WinFormsUtil.GetUnderlyingControl<PictureBox>(sender);
            if (pb == null)
                return;
            var index = SlotPictureBoxes.IndexOf(pb);
            var item = GetItem(index);
            for (int i = 0; i < SlotPictureBoxes.Count; i++)
            {
                if (i == index)
                    continue;
                var dest = GetItem(i);
                dest.CopyFrom(item);
                SlotPictureBoxes[i].BackgroundImage = Sprites.GetImage(item, L_ItemName.Font);
            }
            System.Media.SystemSounds.Asterisk.Play();
        }

        private int GetPageJump()
        {
            return ModifierKeys switch
            {
                Keys.Control => 10,
                Keys.Alt => 25,
                Keys.Shift => 1000,
                _ => 1
            };
        }

        private void B_Up_Click(object sender, EventArgs e)
        {
            if (Page == 0)
                return;
            Page = Math.Max(0, Page - GetPageJump());
            ChangePage();
        }

        private void B_Down_Click(object sender, EventArgs e)
        {
            if (ItemsPerPage * (Page + 1) == Count)
                return;
            Page = Math.Min(PageCount - 1, Page + GetPageJump());
            ChangePage();
        }

        private int PageCount => Count / ItemsPerPage;

        private void ChangePage()
        {
            bool hasPages = Count > ItemsPerPage;
            B_Up.Visible = hasPages && Page > 0;
            B_Down.Visible = hasPages && Page < PageCount;
            L_Page.Visible = hasPages;

            L_Page.Text = $"{Page + 1}/{Count / ItemsPerPage}";
            LoadItems();
        }

        public void LoadItems()
        {
            for (int i = 0; i < SlotPictureBoxes.Count; i++)
            {
                var item = GetItem(i);
                SlotPictureBoxes[i].BackgroundImage = Sprites.GetImage(item, L_ItemName.Font);
            }
        }

        private void B_Clear_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < SlotPictureBoxes.Count; i++)
            {
                var item = GetItem(i);
                item.Delete();
            }
            LoadItems();
            System.Media.SystemSounds.Asterisk.Play();
        }
    }
}
