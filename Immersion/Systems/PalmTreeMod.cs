﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Immersion;
using Immersion.Utility;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Immersion
{
    public class BlockPalmTree : Block
    {
        public static BlockPos[] bottomOffsets;
        public static BlockPos[] offsets;
        public static BlockPos[] cardinaloffsets;
        ICoreAPI Api { get => this.api; }

        LCGRandom rand;

        static readonly string[] parts = new string[]
        {
            "bottom", "middle", "top"
        };
        static readonly string[] directions = new string[]
        {
            "north", "west", "south", "east"
        };
        static readonly int maxTreeSize = parts.Length + 10;

        Block[] trunk;
        public Block[] frond;
        public Block[][] fruits;

        Block tip;
        ItemAxe axe;

        public override void OnLoaded(ICoreAPI Api)
        {
            base.OnLoaded(Api);
            axe = new ItemAxe();
            //if (LastCodePart(1) != "bottom") return;

            bottomOffsets = AreaMethods.AreaBelowOffsetList().ToArray();
            offsets = AreaMethods.AreaAroundOffsetList().ToArray();
            cardinaloffsets = AreaMethods.CardinalOffsetList().ToArray();

            rand = new LCGRandom(Api.World.Seed);

            List<Block> trunkblocks = new List<Block>();
            List<Block> frondblocks = new List<Block>();

            List<Block> nannerblocks = new List<Block>();
            List<Block> cocoblocks = new List<Block>();

            for (int i = 0; i < parts.Length; i++)
            {
                trunkblocks.Add(Api.World.BlockAccessor.GetBlock(CodeWithPart(parts[i], 1)));
            }
            trunk = trunkblocks.ToArray();

            for (int i = 0; i < directions.Length; i++)
            {
                frondblocks.Add(Api.World.BlockAccessor.GetBlock(new AssetLocation("immersion:palmfrond-1-grown-" + directions[i])));
                nannerblocks.Add(Api.World.BlockAccessor.GetBlock(new AssetLocation("immersion:palmfruits-bananna-" + directions[i])));
                cocoblocks.Add(Api.World.BlockAccessor.GetBlock(new AssetLocation("immersion:palmfruits-coconut-" + directions[i])));
            }
            frond = frondblocks.ToArray();
            fruits = new Block[][] { nannerblocks.ToArray(), cocoblocks.ToArray(), null };

            tip = Api.World.BlockAccessor.GetBlock(CodeWithPart("tip", 1));
        }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos Pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            base.OnBlockBroken(world, Pos, byPlayer, dropQuantityMultiplier);
            ItemSlot itemslot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if (FirstCodePart() == "palmlog")
            {
                if (itemslot.Itemstack == null || !Code.ToString().Contains("grown") || !world.Side.IsServer()) return;
                if (itemslot.Itemstack.Collectible.Tool == EnumTool.Axe)
                {
                    for (int x = -2; x <= 2; x++)
                    {
                        for (int z = -2; z <= 2; z++)
                        {
                            for (int y = 0; y <= 16; y++)
                            {
                                BlockPos bPos = new BlockPos(Pos.X + x, Pos.Y + y, Pos.Z + z);
                                Block bBlock = Api.World.BlockAccessor.GetBlock(bPos);
                                if (bBlock is BlockPalmTree)
                                {
                                    if (itemslot.Itemstack == null) return;

                                    world.BlockAccessor.SetBlock(0, bPos);

                                    foreach (ItemStack item in GetDrops(world, bPos, byPlayer))
                                    {
                                        world.SpawnItemEntity(item, bPos.ToVec3d());
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public override bool TryPlaceBlockForWorldGen(IBlockAccessor blockAccessor, BlockPos Pos, BlockFacing onBlockFace, LCGRandom worldgenRandom)
        {
            int fruit = (int)Math.Round(worldgenRandom.NextDouble() * 2.0);            
            GenPalmTree(blockAccessor, Pos, fruit);
            return true;
        }

        public void GenPalmTree(IBlockAccessor blockAccessor, BlockPos Pos, int fruit)
        {
            Block block = blockAccessor.GetBlock(Pos.DownCopy());
            if (block.FirstCodePart() == "sand")
            {
                for (int i = 0; i < bottomOffsets.Length; i++)
                {
                    Block d = blockAccessor.GetBlock(Pos.X + bottomOffsets[i].X, Pos.Y + bottomOffsets[i].Y, Pos.Z + bottomOffsets[i].Z);
                    if (d.LiquidCode == "seawater")
                    {
                        for (int k = 0; k < offsets.Length; k++)
                        {
                            Block c = blockAccessor.GetBlock(Pos.X + offsets[i].X, Pos.Y + offsets[i].Y, Pos.Z + offsets[i].Z);
                            if (c.Class == Class) return;
                        }
                        rand.InitPositionSeed(Pos.X, Pos.Y);
                        Block[] stretchedTrunk = trunk.Stretch((int)(rand.NextDouble() * (maxTreeSize)));

                        BlockPos top = new BlockPos(Pos.X, Pos.Y + stretchedTrunk.Length, Pos.Z);
                        for (int j = 0; j < stretchedTrunk.Length; j++)
                        {
                            blockAccessor.SetBlock(stretchedTrunk[j].BlockId, new BlockPos(Pos.X, Pos.Y + j, Pos.Z));
                        }
                        blockAccessor.SetBlock(tip.BlockId, top);
                        blockAccessor.SpawnBlockEntity("PalmTree", top);

                        GenFrondAndFruits(top, blockAccessor, stretchedTrunk.Length, fruit);
                        break;
                    }
                }
            }
            return;
        }

        public void GenFrondAndFruits(BlockPos Pos, IBlockAccessor blockAccessor, double treesize, int f)
        {
            double scalar = (treesize - parts.Length) / maxTreeSize;
            int palmnum = (int)Math.Round((scalar * 3.0));

            for (int i = 0; i < cardinaloffsets.Length; i++)
            {
                BlockPos vPos = new BlockPos(Pos.X + cardinaloffsets[i].X, Pos.Y + cardinaloffsets[i].Y, Pos.Z + cardinaloffsets[i].Z);
                BlockPos dPos = new BlockPos(Pos.X + cardinaloffsets[i].X, Pos.Y + cardinaloffsets[i].Y - 1, Pos.Z + cardinaloffsets[i].Z);
                Block block = blockAccessor.GetBlock(vPos);
                Block dblock = blockAccessor.GetBlock(dPos);

                if (block.IsReplacableBy(this))
                {
                    Block zBlock = blockAccessor.GetBlock(frond[i].CodeWithPart(palmnum.ToString(), 1));

                    if (zBlock != null) blockAccessor.SetBlock(zBlock.BlockId, vPos);
                }

                if (dblock.IsReplacableBy(this) && fruits[f] != null)
                {
                    blockAccessor.SetBlock(fruits[f][i].BlockId, dPos);
                }
            }
        }
    }

    public class BEPalmTree : BlockEntity
    {
        public BlockPos[] myTree;
        public BlockPos[] myFronds;
        public BlockPos[] myFruits;
        public Block myFruit;
        public BlockPalmTree palmtree;
        public int fruitnum;

        public double nextGrowTime;

        public override void Initialize(ICoreAPI Api)
        {
            base.Initialize(Api);
            UpdateTreeSize();
            UpdateFruitsFronds();

            palmtree = Api.World.BlockAccessor.GetBlock(Pos) as BlockPalmTree;
            if (Api.World.Side.IsServer()) RegisterGameTickListener(CheckTree, 1000);
            if (Api.World.Calendar.TotalHours > nextGrowTime) UpdateGrowTime();

            if (myFruits == null || myFruits[0] == null) return;

            myFruit = Api.World.BlockAccessor.GetBlock(myFruits[0]);
            for (int i = 0; i < palmtree.fruits.Length; i++)
            {
                if (palmtree.fruits[i] == null) return;

                if (palmtree.fruits[i].FirstOrDefault().FirstCodePart(1) == myFruit.FirstCodePart(1))
                {
                    fruitnum = i;
                    break;
                }
            }
        }

        public void CheckTree(float dt)
        {
            if (myTree == null || myTree.Length == 0)
            {
                Api.World.BlockAccessor.RemoveBlockEntity(Pos);
                return;
            }

            for (int i = 0; i < myTree.Length; i++)
            {
                if (myTree[i] == null) continue;

                Block checkedBlock = Api.World.BlockAccessor.GetBlock(myTree[i]);
                if (!(checkedBlock is BlockPalmTree))
                {
                    UpdateTreeSize();
                    break;
                }
            }

            if (nextGrowTime < Api.World.Calendar.TotalHours)
            {
                for (int i = 0; i < BlockPalmTree.cardinaloffsets.Length; i++)
                {
                    BlockPos v = BlockPalmTree.cardinaloffsets[i];
                    BlockPos palmPos = new BlockPos(Pos.X + v.X, Pos.Y + v.Y, Pos.Z + v.Z);
                    BlockPos fruitPos = new BlockPos(Pos.X + v.X, Pos.Y + v.Y - 1, Pos.Z + v.Z);
                    Block fruitspace = Api.World.BlockAccessor.GetBlock(fruitPos);
                    Block palmspace = Api.World.BlockAccessor.GetBlock(palmPos);

                    if (fruitspace.Id == 0) Api.World.BlockAccessor.SetBlock(palmtree.fruits[fruitnum][i].BlockId, fruitPos);
                    if (palmspace.Id == 0) Api.World.BlockAccessor.SetBlock(palmtree.frond[i].BlockId, palmPos);
                }
                UpdateGrowTime();
            }
        }

        public void UpdateGrowTime()
        {
            nextGrowTime = Api.World.Calendar.TotalHours + 48;
        }

        public void UpdateTreeSize()
        {
            List<BlockPos> mytree = new List<BlockPos>();

            for (int x = -2; x <= 2; x++)
            {
                for (int y = 0; y < 20; y++)
                {
                    for (int z = -2; z <= 2; z++)
                    {
                        BlockPos bPos = new BlockPos(Pos.X + x, Pos.Y - y, Pos.Z + z);
                        Block bBlock = Api.World.BlockAccessor.GetBlock(bPos);

                        if (bBlock is BlockPalmTree)
                        {
                            mytree.Add(bPos);
                        }
                    }
                }
            }
            myTree = mytree.ToArray();
        }

        public void UpdateFruitsFronds()
        {
            if (myFruits == null || myFronds == null)
            {
                for (int i = 0; i < BlockPalmTree.cardinaloffsets.Length; i++)
                {
                    BlockPos v = BlockPalmTree.cardinaloffsets[i];
                    BlockPos frondPos = new BlockPos(Pos.X + v.X, Pos.Y + v.Y, Pos.Z + v.Z);
                    BlockPos fruitPos = new BlockPos(Pos.X + v.X, Pos.Y + v.Y - 1, Pos.Z + v.Z);

                    Block frondBlock = Api.World.BlockAccessor.GetBlock(frondPos);
                    Block fruitBlock = Api.World.BlockAccessor.GetBlock(fruitPos);

                    if (frondBlock.FirstCodePart() == "palmfrond")
                    {
                        if (myFronds == null) myFronds = new BlockPos[4];
                        myFronds[i] = frondPos;
                    }
                    if (fruitBlock.FirstCodePart() == "palmfruits")
                    {
                        if (myFruits == null) myFruits = new BlockPos[4];
                        myFruits[i] = fruitPos;
                    }
                }
            }
        }
    }
}
