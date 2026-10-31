using System;
using System.Reflection;
using System.Runtime.InteropServices;
using ParoleRP;
class NativeKeyboardTests {
 static int checks;
 static void Check(bool result,string name){if(!result)throw new Exception("FAIL : "+name);checks++;Console.WriteLine("PASS "+name);}
 static object Field(object value,string field){return value.GetType().GetField(field).GetValue(value);}
 static void Main(){try{foreach(ushort key in new ushort[]{67,86}){var packets=(Array)typeof(Native).GetMethod("BuildChord",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{key});Check(packets.Length==4,"Ctrl+"+(char)key+" contient quatre événements");ushort[] expected={17,key,key,17};uint[] flags={0,0,2,2};for(int i=0;i<4;i++){object packet=packets.GetValue(i);Check(Convert.ToUInt32(Field(packet,"type"))==1,"événement "+i+" déclaré INPUT_KEYBOARD, pas INPUT_MOUSE");object keyboard=Field(Field(packet,"u"),"ki");Check(Convert.ToUInt16(Field(keyboard,"vk"))==expected[i]&&Convert.ToUInt32(Field(keyboard,"flags"))==flags[i],"ordre et relâchement clavier "+i);}Type layout=packets.GetValue(0).GetType();Check(Marshal.SizeOf(layout)==(IntPtr.Size==8?40:28),"taille INPUT conforme à l’ABI Windows");Check(Marshal.OffsetOf(layout,"u").ToInt32()==(IntPtr.Size==8?8:4),"alignement de l’union Windows");}Console.WriteLine(checks+" vérifications de paquets clavier réussies. Aucun appui réel n’a été envoyé.");}catch(Exception e){Console.WriteLine(e.Message);Environment.ExitCode=1;}}
}
