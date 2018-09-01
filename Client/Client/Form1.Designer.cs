namespace Client
{
    partial class ClientForm
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.listView1 = new System.Windows.Forms.ListView();
            this.name = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.size = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.type = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.mtime = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.status = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.UP_button = new System.Windows.Forms.Button();
            this.DOWN_button2 = new System.Windows.Forms.Button();
            this.DEL_button = new System.Windows.Forms.Button();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.StopLoad_button = new System.Windows.Forms.Button();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.InforichTextBox = new System.Windows.Forms.RichTextBox();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.listView1);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(679, 244);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "My Files";
            // 
            // listView1
            // 
            this.listView1.AllowDrop = true;
            this.listView1.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.listView1.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.name,
            this.size,
            this.type,
            this.mtime,
            this.status});
            this.listView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listView1.FullRowSelect = true;
            this.listView1.GridLines = true;
            this.listView1.Location = new System.Drawing.Point(3, 25);
            this.listView1.Name = "listView1";
            this.listView1.Size = new System.Drawing.Size(673, 216);
            this.listView1.TabIndex = 7;
            this.listView1.UseCompatibleStateImageBehavior = false;
            this.listView1.View = System.Windows.Forms.View.Details;
            this.listView1.DragDrop += new System.Windows.Forms.DragEventHandler(this.listView1_DragDrop);
            this.listView1.DragEnter += new System.Windows.Forms.DragEventHandler(this.listView1_DragEnter);
            // 
            // name
            // 
            this.name.Text = "名称";
            this.name.Width = 130;
            // 
            // size
            // 
            this.size.Text = "大小";
            this.size.Width = 104;
            // 
            // type
            // 
            this.type.Text = "类型";
            this.type.Width = 98;
            // 
            // mtime
            // 
            this.mtime.Text = "修改时间";
            this.mtime.Width = 142;
            // 
            // status
            // 
            this.status.Text = "文件状态";
            this.status.Width = 196;
            // 
            // UP_button
            // 
            this.UP_button.Location = new System.Drawing.Point(6, 29);
            this.UP_button.Name = "UP_button";
            this.UP_button.Size = new System.Drawing.Size(122, 39);
            this.UP_button.TabIndex = 2;
            this.UP_button.Text = "SelectFile";
            this.UP_button.UseVisualStyleBackColor = true;
            this.UP_button.Click += new System.EventHandler(this.UP_button_Click);
            // 
            // DOWN_button2
            // 
            this.DOWN_button2.Location = new System.Drawing.Point(187, 29);
            this.DOWN_button2.Name = "DOWN_button2";
            this.DOWN_button2.Size = new System.Drawing.Size(122, 39);
            this.DOWN_button2.TabIndex = 3;
            this.DOWN_button2.Text = "DownLoad";
            this.DOWN_button2.UseVisualStyleBackColor = true;
            this.DOWN_button2.Click += new System.EventHandler(this.DOWN_button2_Click);
            // 
            // DEL_button
            // 
            this.DEL_button.Location = new System.Drawing.Point(549, 29);
            this.DEL_button.Name = "DEL_button";
            this.DEL_button.Size = new System.Drawing.Size(122, 39);
            this.DEL_button.TabIndex = 4;
            this.DEL_button.Text = "SelectData";
            this.DEL_button.UseVisualStyleBackColor = true;
            this.DEL_button.Click += new System.EventHandler(this.DEL_button_Click);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // StopLoad_button
            // 
            this.StopLoad_button.Location = new System.Drawing.Point(368, 29);
            this.StopLoad_button.Name = "StopLoad_button";
            this.StopLoad_button.Size = new System.Drawing.Size(122, 39);
            this.StopLoad_button.TabIndex = 6;
            this.StopLoad_button.Text = "UpdataFile";
            this.StopLoad_button.UseVisualStyleBackColor = true;
            this.StopLoad_button.Click += new System.EventHandler(this.StopLoad_button_Click);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.StopLoad_button);
            this.groupBox2.Controls.Add(this.DEL_button);
            this.groupBox2.Controls.Add(this.UP_button);
            this.groupBox2.Controls.Add(this.DOWN_button2);
            this.groupBox2.Location = new System.Drawing.Point(12, 420);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(679, 86);
            this.groupBox2.TabIndex = 7;
            this.groupBox2.TabStop = false;
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.InforichTextBox);
            this.groupBox3.Location = new System.Drawing.Point(12, 262);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(679, 152);
            this.groupBox3.TabIndex = 8;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Info";
            // 
            // InforichTextBox
            // 
            this.InforichTextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.InforichTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.InforichTextBox.Location = new System.Drawing.Point(3, 25);
            this.InforichTextBox.Name = "InforichTextBox";
            this.InforichTextBox.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            this.InforichTextBox.Size = new System.Drawing.Size(673, 124);
            this.InforichTextBox.TabIndex = 1;
            this.InforichTextBox.Text = "";
            // 
            // ClientForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 21F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(706, 518);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.groupBox2);
            this.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.ForeColor = System.Drawing.Color.Black;
            this.Margin = new System.Windows.Forms.Padding(5);
            this.MaximizeBox = false;
            this.Name = "ClientForm";
            this.Text = "Client";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ClientForm_FormClosing);
            this.Load += new System.EventHandler(this.ClientForm_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.groupBox3.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button UP_button;
        private System.Windows.Forms.Button DOWN_button2;
        private System.Windows.Forms.Button DEL_button;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.SaveFileDialog saveFileDialog1;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
        private System.Windows.Forms.Button StopLoad_button;
        private System.Windows.Forms.ListView listView1;
        private System.Windows.Forms.ColumnHeader name;
        private System.Windows.Forms.ColumnHeader size;
        private System.Windows.Forms.ColumnHeader type;
        private System.Windows.Forms.ColumnHeader mtime;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.ColumnHeader status;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.RichTextBox InforichTextBox;
    }
}

