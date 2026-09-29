using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ReklamHesap;

public class MainForm : Form
{
    readonly TabControl tabs = new();
    ComboBox product = new(), sub = new();
    NumericUpDown width = Num(), height = Num(), quantity = Num();
    Label area = new(), materialTotal = new(), serviceTotal = new(), grandTotal = new();
    DataGridView priceGrid = new(), serviceGrid = new();

    public MainForm()
    {
        Text = "Reklam Hesaplama";
        StartPosition = FormStartPosition.CenterScreen;
        Width = 1200; Height = 760;
        MinimumSize = new Size(1050,650);
        Build();
    }

    static NumericUpDown Num() => new() { Minimum=0, Maximum=100000, DecimalPlaces=2, Increment=.1M, Width=220 };

    void Build()
    {
        var top = new Panel { Dock=DockStyle.Top, Height=70, Padding=new Padding(20) };
        top.Controls.Add(new Label {
            Text="REKLAM HESAPLAMA", AutoSize=true,
            Font=new Font("Segoe UI",20,FontStyle.Bold), Location=new Point(20,16)
        });
        tabs.Dock=DockStyle.Fill;
        tabs.TabPages.Add(FolyoPage());
        tabs.TabPages.Add(CompositePage());
        tabs.TabPages.Add(PricesPage());
        tabs.TabPages.Add(ServicesPage());
        Controls.Add(tabs); Controls.Add(top);
    }

    TabPage FolyoPage()
    {
        var p=new TabPage("Folyo Hesaplama");
        var g=new TableLayoutPanel { Dock=DockStyle.Top, Padding=new Padding(25), Height=500,
            ColumnCount=2, RowCount=11 };
        g.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,230));
        g.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));

        L(g,"Folyo Türü",0);
        product.DropDownStyle=ComboBoxStyle.DropDownList;
        product.SelectedIndexChanged += (_,_) => {
            sub.Visible = product.SelectedItem?.ToString()=="Laminasyon";
            Recalc();
        };
        g.Controls.Add(product,1,0);

        L(g,"Laminasyon",1);
        sub.DropDownStyle=ComboBoxStyle.DropDownList;
        sub.Items.AddRange(new object[]{"Laminasyonlu","Laminasyonsuz"});
        sub.SelectedIndex=0;
        g.Controls.Add(sub,1,1);

        L(g,"En (cm)",2); g.Controls.Add(width,1,2);
        L(g,"Boy (cm)",3); g.Controls.Add(height,1,3);
        L(g,"Adet",4); quantity.Value=1; quantity.DecimalPlaces=0; g.Controls.Add(quantity,1,4);

        L(g,"Toplam Alan",5);
        area.Text="0,00 m²"; area.Font=new Font("Segoe UI",14,FontStyle.Bold); g.Controls.Add(area,1,5);

        L(g,"Malzeme Tutarı",6); g.Controls.Add(materialTotal,1,6);
        L(g,"Ek Hizmetler",7); g.Controls.Add(serviceTotal,1,7);
        L(g,"TOPLAM MALİYET",8);
        grandTotal.Font=new Font("Segoe UI",16,FontStyle.Bold); g.Controls.Add(grandTotal,1,8);

        var calc=new Button { Text="HESAPLA", Width=180, Height=45 };
        calc.Click+=(_,_)=>Recalc(); g.Controls.Add(calc,1,9);

        foreach(var n in new[]{width,height,quantity}) n.ValueChanged+=(_,_)=>Recalc();
        sub.SelectedIndexChanged+=(_,_)=>Recalc();

        p.Controls.Add(g);
        LoadProducts();
        return p;
    }

    void LoadProducts()
    {
        var rows=Database.Products();
        product.Items.Clear();
        foreach(var n in rows.Select(x=>x.Name).Distinct()) product.Items.Add(n);
        if(product.Items.Count>0) product.SelectedIndex=0;
    }

    decimal SelectedPrice()
    {
        var rows=Database.Products();
        string name=product.SelectedItem?.ToString() ?? "";
        string? option= name=="Laminasyon" ? sub.SelectedItem?.ToString() : null;
        var row=rows.FirstOrDefault(x=>x.Name==name && x.SubOption==option);
        return row?.Price ?? 0;
    }

    void Recalc()
    {
        decimal m2=(width.Value/100m)*(height.Value/100m)*quantity.Value;
        area.Text=$"{m2:N2} m²";
        decimal mt=m2*SelectedPrice();
        materialTotal.Text=$"{mt:N2} TL";

        // Ek hizmetler fiyatları ayrı yönetildiği için bu ilk sürümde 0 gösterilir.
        serviceTotal.Text="0,00 TL";
        grandTotal.Text=$"{mt:N2} TL";
    }

    TabPage PricesPage()
    {
        var p=new TabPage("Fiyat Yönetimi");
        priceGrid.Dock=DockStyle.Fill; priceGrid.AllowUserToAddRows=false;
        priceGrid.AutoGenerateColumns=false;
        priceGrid.Columns.Add(new DataGridViewTextBoxColumn{HeaderText="Kategori",DataPropertyName="Category",ReadOnly=true});
        priceGrid.Columns.Add(new DataGridViewTextBoxColumn{HeaderText="Ürün",DataPropertyName="Name",ReadOnly=true});
        priceGrid.Columns.Add(new DataGridViewTextBoxColumn{HeaderText="Seçenek",DataPropertyName="SubOption",ReadOnly=true});
        priceGrid.Columns.Add(new DataGridViewTextBoxColumn{HeaderText="Birim",DataPropertyName="Unit",ReadOnly=true});
        priceGrid.Columns.Add(new DataGridViewTextBoxColumn{HeaderText="Fiyat (TL)",DataPropertyName="Price"});
        priceGrid.CellEndEdit+=(_,e)=>{
            if(e.RowIndex<0)return;
            var row=priceGrid.Rows[e.RowIndex].DataBoundItem as ProductRow;
            if(row!=null && decimal.TryParse(Convert.ToString(priceGrid.Rows[e.RowIndex].Cells[4].Value),out var v))
                Database.SetProductPrice(row.Id,v);
        };
        var refresh=new Button{Text="Fiyatları Yenile",Dock=DockStyle.Top,Height=40};
        refresh.Click+=(_,_)=>LoadPriceGrid();
        p.Controls.Add(priceGrid); p.Controls.Add(refresh);
        LoadPriceGrid();
        return p;
    }

    void LoadPriceGrid()
    {
        priceGrid.DataSource=null;
        priceGrid.DataSource=Database.Products();
    }

    TabPage ServicesPage()
    {
        var p=new TabPage("Ek Hizmet Fiyatları");
        serviceGrid.Dock=DockStyle.Fill; serviceGrid.AllowUserToAddRows=false;
        serviceGrid.AutoGenerateColumns=false;
        serviceGrid.Columns.Add(new DataGridViewTextBoxColumn{HeaderText="Hizmet",DataPropertyName="Name",ReadOnly=true});
        serviceGrid.Columns.Add(new DataGridViewTextBoxColumn{HeaderText="Birim",DataPropertyName="Unit",ReadOnly=true});
        serviceGrid.Columns.Add(new DataGridViewTextBoxColumn{HeaderText="Fiyat (TL)",DataPropertyName="Price"});
        serviceGrid.CellEndEdit+=(_,e)=>{
            if(e.RowIndex<0)return;
            var row=serviceGrid.Rows[e.RowIndex].DataBoundItem as ServiceRow;
            if(row!=null && decimal.TryParse(Convert.ToString(serviceGrid.Rows[e.RowIndex].Cells[2].Value),out var v))
                Database.SetServicePrice(row.Id,v);
        };
        var refresh=new Button{Text="Hizmet Fiyatlarını Yenile",Dock=DockStyle.Top,Height=40};
        refresh.Click+=(_,_)=>LoadServices();
        p.Controls.Add(serviceGrid); p.Controls.Add(refresh);
        LoadServices();
        return p;
    }

    void LoadServices(){ serviceGrid.DataSource=null; serviceGrid.DataSource=Database.Services(); }

    TabPage CompositePage()
    {
        var p=new TabPage("Kompozit");
        var info=new Label{
            Text="Kompozit: marka + mm kalınlık + tabaka ölçüsü + tabaka fiyatı + fire hesabı bir sonraki modülde bağlanacak.",
            AutoSize=true, Location=new Point(25,30), Font=new Font("Segoe UI",12)};
        p.Controls.Add(info); return p;
    }

    static void L(TableLayoutPanel g,string s,int row)
    {
        g.Controls.Add(new Label{Text=s,AutoSize=true,Anchor=AnchorStyles.Left,
            Font=new Font("Segoe UI",10,FontStyle.Bold)},0,row);
    }
}
