using System;
using System.Windows.Controls;
namespace ParoleRP {
 public sealed partial class Desktop {
  void CreateProfile(){if(!Leave())return;var character=new Character();data.Characters.Add(character);if(data.Characters.Count==1)data.ActiveId=character.Id;editId=character.Id;revision++;Persist();Header();Render();}
  void EmptyProfilePage(string heading){host.Children.Add(T(heading,31));host.Children.Add(Space(22));host.Children.Add(Card(S(T("Aucun personnage",20),Space(12),Row(Button("Créer un personnage",CreateProfile,true),Button("Importer un profil",()=>Go(5))))));}
 }
}
