using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.IO.Compression;

namespace NST_Pak_Explorer {
    public class IGA {
        private Int32 signature;
        public Int32 version;
        private Int32 info_size;
        private Int32 files_count;
        private Int32 chunk_size;
        private Int32 magic_number1;
        private Int32 magic_number2;
        private Int32 zero1;
        private Int32 table1_size;
        private Int32 table2_size;
        private UInt32 names_offset;
        private Int32 zero2;
        private Int32 names_size;
        private Int32 one;

        public List<File> file = new List<File>();
        public List<UInt16> mc_table = new List<ushort>();
        public List<UInt16> sc_table = new List<ushort>();
        public IGA(String path, EndiannessAwareBinaryReader.Endianness endianness) {
            FileStream iga_file = new FileStream(path, FileMode.Open, FileAccess.Read);
            EndiannessAwareBinaryReader reader = new EndiannessAwareBinaryReader(iga_file, endianness);
            signature = reader.ReadInt32();
            version = reader.ReadInt32();
            info_size = reader.ReadInt32();
            files_count = reader.ReadInt32();
            chunk_size = reader.ReadInt32();
            magic_number1 = reader.ReadInt32();
            magic_number2 = reader.ReadInt32();
            zero1 = reader.ReadInt32();
            table1_size = reader.ReadInt32();
            table2_size = reader.ReadInt32();
            if (version == 11 || version == 10) //Skylanders SuperChargers WiiU Big Endian
            {
                zero2 = reader.ReadInt32();
                names_offset = reader.ReadUInt32();
            }
            else
            {
                names_offset = reader.ReadUInt32();
                zero2 = reader.ReadInt32();
            }
            names_size = reader.ReadInt32();
            one = reader.ReadInt32();
            for (int i = 0; i < files_count; ++i) {
                file.Add(new File());
                file[i].setID(reader.ReadInt32());
                file[i].setSource(path);
            }
            for (int i = 0; i < files_count; ++i) {
                if (version == 11 || version == 10)
                {
                    file[i].setOrdinal(reader.ReadInt32());
                    file[i].setOffset(reader.ReadUInt32());
                }
                else
                {
                    file[i].setOffset(reader.ReadUInt32());
                    file[i].setOrdinal(reader.ReadInt32());
                }
                file[i].setSize(reader.ReadInt32());
                file[i].setCompression(reader.ReadInt32());
                file[i].setSourceOffset(file[i].getOffset());
            }
            for (int i = 0; i < table1_size; ++i) {
                mc_table.Add(reader.ReadUInt16());
            }
            for (int i = 0; i < table2_size / 2; ++i) {
                sc_table.Add(reader.ReadUInt16());
            }
            //Size: 920944
            //Ordinal: 606208
            // max: 33 499
            reader.BaseStream.Position = names_offset;
            Int32[] name_offsets = new Int32[files_count];
            for (int i = 0; i < files_count; ++i) {
                name_offsets[i] = reader.ReadInt32();
            }
            for (int i = 0; i < files_count; ++i) {
                reader.BaseStream.Position = names_offset + name_offsets[i];
                if (version == 10)
                {
                    String rel_name = "";
                    rel_name = readString(reader);
                    file[i].setFullName(rel_name);
                    file[i].setRelName(rel_name);
                }
                else
                {
                    String full_name = ""; String rel_name = "";
                    full_name = readString(reader);
                    rel_name = readString(reader);
                    file[i].setFullName(full_name);
                    file[i].setRelName(rel_name);
                }
            }
            reader.Close();
        }

        public void repack(String path, System.Windows.Forms.ProgressBar bar, EndiannessAwareBinaryReader.Endianness endianness) {
            for (int i = 0; i < files_count; ++i) {
                if (path == file[i].getSource()) {
                    throw new Exception("Can't overwrite source!");
                }
            }
            FileStream repacked_file = new FileStream(path, FileMode.Create, FileAccess.Write);
            BinaryWriter writer = new BinaryWriter(repacked_file);

            recalculate();
            writer.Write(signature);
            writer.Write(version);
            writer.Write(info_size);
            writer.Write(files_count);
            writer.Write(chunk_size);
            writer.Write(magic_number1);
            writer.Write(magic_number2);
            writer.Write(zero1);
            writer.Write(table1_size);
            writer.Write(table2_size);
            writer.Write(names_offset);
            writer.Write(zero2);
            writer.Write(names_size);
            writer.Write(one);
            
            for (int i = 0; i < files_count; ++i) {
                writer.Write(file[i].getID());
            }
            for (int i = 0; i < files_count; ++i) {
                writer.Write(file[i].getOffset());
                writer.Write(file[i].getOrdinal());
                writer.Write(file[i].getSize());
                writer.Write(0xFFFFFFFF);
            }
            if (bar != null) bar.Maximum = files_count;
            for (int i = 0; i < files_count; ++i) {
                if (bar != null) bar.Value = i;
                System.Windows.Forms.Application.DoEvents();
                FileStream source = new FileStream(file[i].getSource(),FileMode.Open,FileAccess.Read);
                EndiannessAwareBinaryReader reader = new EndiannessAwareBinaryReader(source, endianness);
                reader.BaseStream.Position = file[i].getSourceOffset();
                writer.BaseStream.Position = file[i].getOffset();
                if (file[i].getCompression() == Compression.NONE) {
                    writer.Write(reader.ReadBytes(file[i].getSize()));
                } else {
                    uncomress(reader, writer, file[i].getSize());
                    file[i].setCompression(Compression.NONE);
                }
                reader.Close();
                
            }
            writer.BaseStream.Position = names_offset;
            int cur_offset = files_count * 4;
            for (int i = 0; i < files_count; ++i) {
                writer.Write(cur_offset);
                cur_offset += file[i].getFullName().Length + file[i].getRelName().Length + 6; 
            }
            for (int i = 0; i < files_count; ++i) {
                String full_name = file[i].getFullName();
                String rel_name = file[i].getRelName();
                foreach (Char c in full_name) writer.Write(c);
                writer.Write('\0');
                foreach (Char c in rel_name) writer.Write(c);
                writer.Write('\0');
                writer.Write((int)0);
            }
            writer.Close();
        }
        public void replace(Int32 index, String source, UInt32 offset, Int32 size) {
            UInt32 old_size = (UInt32)file[index].getSize();
            file[index].setSize(size);
            file[index].setSource(source);
            file[index].setSourceOffset(offset);
            file[index].setCompression(-1);
            UInt32 new_size = (UInt32)file[index].getSize();
            UInt32 delta_offset = round(new_size) - round(old_size);
            names_offset += (UInt32)delta_offset;
            for (int i = 0; i < files_count; ++i) {
                if (file[i].getOffset() > file[index].getOffset()) {
                    file[i].setOffset(file[i].getOffset() + (UInt32)delta_offset);
                }
            }
        }
        public void add(File file) {
            for (int i = 0; i < files_count; ++i) {
                if (this.file[i].getID() == file.getID()) {
                    replace(i, file.getSource(), file.getSourceOffset(), file.getSize());
                    return;
                }
            }
            uint offset = 0;
            int ordinal = 0;
            for (int i = 0; i < files_count; ++i) {
                if (this.file[i].getOffset() + this.file[i].getSize() > offset) offset = this.file[i].getOffset() + (UInt32)this.file[i].getSize();
                if (this.file[i].getOrdinal() > ordinal) ordinal = this.file[i].getOrdinal();
            }
            file.setOffset(round(offset));
            file.setOrdinal(ordinal + 1);
            ++files_count;
 
            this.file.Add(file);

            recalculate();
        }
        private void recalculate() {
            info_size = files_count * 20;
            uint cur_offset = round((UInt32)info_size);
            for (int i = 0; i < files_count; ++i) {
                file[i].setOffset(cur_offset);
                cur_offset += round((UInt32)file[i].getSize());
            }
            names_offset = cur_offset;
        }
        public void normalize(String path) {
            path = path.Replace('\\', '/');
            if (path.Last() != '/') path += '/';
            info_size = files_count * 20 ;
            uint cur_offset = round((UInt32)info_size +56);
            for (int i = 0; i < files_count; ++i) {
                replace(i, path + file[i].getFullName(), 0, file[i].getSize());
                file[i].setCompression(Compression.NONE);
                file[i].setOffset(cur_offset);
                cur_offset += round((UInt32)file[i].getSize());
            }
            names_offset = cur_offset;
        }
        public void uncomress(EndiannessAwareBinaryReader reader, BinaryWriter writer, Int32 size) {
            bool accurate = true;
            SevenZip.Compression.LZMA.Decoder coder = new SevenZip.Compression.LZMA.Decoder();
            Int64 begin = writer.BaseStream.Position;
            Int32 shift_back = version == 12 ? 7 : 9;
            Int32 uncompressed_size = version == 12 ? 0x100000 : 0x8000;
            Int32 properties_value = version == 12 ? 171 : 93;
            while (writer.BaseStream.Position - begin < size) {
                Int32 chunk_size = 0;
                if (!accurate)
                {
                    chunk_size = reader.ReadInt32();
                }
                else
                {
                    chunk_size = (Int32)round((UInt32)reader.ReadInt32()) - shift_back;
                }
                byte[] properties = reader.ReadBytes(5);
                if (properties[0] != properties_value || BitConverter.ToInt32(properties, 1) != uncompressed_size)
                {
                    reader.BaseStream.Position -= shift_back;
                    writer.Write(reader.ReadBytes(uncompressed_size));
                }
                else
                {
                    coder.SetDecoderProperties(properties);
                    coder.Code(reader.BaseStream, writer.BaseStream, chunk_size, Math.Min(uncompressed_size, size - (writer.BaseStream.Position - begin)), null);
                    writer.Write(reader.ReadBytes((int)(round((UInt32)reader.BaseStream.Position) - reader.BaseStream.Position)));
                }
            }
        }
        public void extract(Int32 index, String path, EndiannessAwareBinaryReader.Endianness endianness) {
            FileStream file = new FileStream(this.file[index].getSource(),FileMode.Open,FileAccess.Read);
            EndiannessAwareBinaryReader reader = new EndiannessAwareBinaryReader(file, endianness);
            FileStream ext = new FileStream(path, FileMode.Create, FileAccess.Write);
            BinaryWriter writer = new BinaryWriter(ext);
            reader.BaseStream.Position = this.file[index].getSourceOffset();
            if (this.file[index].getCompression() != Compression.NONE) {
                uncomress(reader,writer,this.file[index].getSize()); // , (int)((uint)this.file[index].getCompressionInt() & (0x1FFFFFFF))
            } else {
                writer.Write(reader.ReadBytes(this.file[index].getSize()));
            }
            
            writer.Close();
        }
        public Int32 getTableOffset(File f) {
            return f.getCompressionInt() & (~0x20000000);
        }
        public List<UInt16> getChunks(File f) {
            if (f.getCompression() == Compression.NONE) return null;
            List<UInt16> arr = new List<ushort>();
            Int32 offset = getTableOffset(f);
            if (f.getSize() < 0x8000) {
                arr.Add(sc_table[offset / 2]);
            } else {
                while (true) {
                    arr.Add(mc_table[offset]);
                    if (offset < mc_table.Count) {
                        if (mc_table[offset] > mc_table[offset+1]) {
                            break;
                        }
                    } else {
                        break;
                    }
                    ++offset;
                }
            }
            return arr;
        }
        private UInt32 round(UInt32 num) { return ((num - 1) / (UInt32)chunk_size + 1) * (UInt32)chunk_size; }
        private bool isNormalized() {
            foreach (File f in file) {
                if (f.getCompression() != Compression.NONE) {
                    return false;
                }
            }
            return true;
        }
        private String readString(EndiannessAwareBinaryReader reader) {
            String str = "";
            Char ch = '\0';
            do {
                ch = reader.ReadChar();
                if (ch != '\0') str += ch;
            } while (ch != '\0');
            return str;
        }
    }

    public class File {
        private Int32 ID;
        private UInt32 offset;
        private Int32 ordinal;
        private Int32 size;
        private Int32 compression;
        private String full_name;
        private String rel_name;
        private Int16[] chunk_offsets;

        private String file_path;
        private UInt32 source_offset;
        public MemoryStream stream;


        public void setChunkOffsets(Int16[] chunk_offsets) { this.chunk_offsets = chunk_offsets; }
        public void setID(Int32 ID) { this.ID = ID; }
        public void setOffset(UInt32 offset) { this.offset = offset; }
        public void setOrdinal(Int32 ordinal) { this.ordinal = ordinal; }
        public void setSize(Int32 size) { this.size = size; }
        public void setCompression(Int32 compression) { this.compression = compression; }
        public void setCompression(Compression compression) {
            switch (compression) {
                case Compression.NONE:
                    this.compression = ~0;
                    break;
                case Compression.LZMA:
                    this.compression = 0x20000000;
                    break;
                case Compression.DEFLATE:
                    this.compression = 0;
                    break;
            }
        }
        public void setRelName(String rel_name) { this.rel_name = rel_name; }
        public void setFullName(String full_name) { this.full_name = full_name; }
        public void setSource (String file_path) { this.file_path = file_path; }
        public void setSourceOffset(UInt32 source_offset) { this.source_offset = source_offset; }

        public Int16[] getChunkOffsets() { return chunk_offsets; }
        public Int32 getID() { return ID; }
        public UInt32 getOffset() { return offset; }
        public Int32 getOrdinal() { return ordinal; }
        public Int32 getSize() { return size; }
        public Int32 getCompressionInt() { return compression; }
        public Compression getCompression() {
            if ((uint)compression == 0xFFFFFFFF) {
                return Compression.NONE;
            } else if (((uint)compression & 0x20000000) != 0) {
                return Compression.LZMA;
            } else {
                return Compression.DEFLATE;
            }
        }
        public String getRelName() { return rel_name; }
        public String getFullName() { return full_name; }
        public String getSource() { return file_path; }
        public UInt32 getSourceOffset() { return source_offset; }
        
    }
    public enum Compression {
        NONE, LZMA, DEFLATE
    }
}
